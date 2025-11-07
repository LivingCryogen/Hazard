using AzProxy.BanList;
using Azure;
using Azure.Data.Tables;
using System.Collections.Concurrent;

namespace AzProxy;

public class StorageManager : IHostedService
{
    private readonly IHostApplicationLifetime _appLife;
    private readonly DateTimeOffset _bootTime = DateTimeOffset.UtcNow;
    private readonly ILogger _logger;
    private readonly IBanCache _cache;
    private readonly TableClient _banTableClient;
    private readonly TableClient _appVarsTableClient;
    private readonly string _banListPartitionKey;
    private readonly string _appVarsPartitionKey;
    private readonly string _dbPruneFlagRowKey;
    private readonly TimeSpan _entryDuration;
    private readonly ConcurrentDictionary<string, ETag> _tagCache = new(); // needed for easy updates
    private readonly SemaphoreSlim _tableSemaphore = new(1, 1);

    public StorageManager(IConfiguration config, IHostApplicationLifetime appLife, ILogger<StorageManager> logger, IBanCache cache)
    {
        _appLife = appLife;
        _logger = logger;
        _cache = cache;

        string? storageConnection = config["StorageConnectionString"];

        if (string.IsNullOrEmpty(storageConnection))
        {
            logger.LogError("Storage access configuration incorrect.");
            throw new NullReferenceException();
        }

        try
        {
            TableServiceClient serviceClient = new(storageConnection);

            string? banTableName = config["BanTableName"];
            if (string.IsNullOrEmpty(banTableName))
                throw new ArgumentException("BanTableName was null or empty. Check configuration (App settings).");
            _banTableClient = serviceClient.GetTableClient(banTableName);

            string? varsTableName = config["VariablesTableName"];
            if (string.IsNullOrEmpty(varsTableName))
                throw new ArgumentException("VarsTableName was null or empty. Check configuration (App settings).");
            _appVarsTableClient = serviceClient.GetTableClient(varsTableName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to construct TableClients due to an error: {message}", ex.Message);
        }
        if (_banTableClient == null)
        {
            throw new NullReferenceException("Failed to construct TableClient.");
        }
        if (_appVarsTableClient == null)
        {
            throw new NullReferenceException("Failed to construct AppVars TableClient.");
        }

        _banListPartitionKey = config["BanlistPartitionKey"] ?? string.Empty;
        _appVarsPartitionKey = config["AppVarsPartitionKey"] ?? string.Empty;
        _dbPruneFlagRowKey = config["DBPruneFlagRowKey"] ?? string.Empty;

        if (_banListPartitionKey == string.Empty)
            logger.LogWarning("Banlist partition key empty.");
        if (_appVarsPartitionKey == string.Empty)
            logger.LogWarning("AppVars partition key empty.");
        if (_dbPruneFlagRowKey == string.Empty)
            logger.LogWarning("DB prune flag row key empty.");

        _entryDuration = int.TryParse(config["EntryDurationDays"], out int result) ? TimeSpan.FromDays(result) : TimeSpan.FromDays(365);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _appLife.ApplicationStopping.Register(OnAppStopping);

        await PopulateCache();
        await GetOrCreateVars();

        return;
    }

    private async Task PopulateCache()
    {
        var recordedBans = await GetRecordsAsync((entry) => entry.NowBanned);
        _cache.Initialize(recordedBans);
    }

    private async Task GetOrCreateVars()
    {

    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OnAppStopping()
    {
        List<Ban> updatedBans = [];
        _logger.LogInformation("Beginning Table Update....");

        try
        {
            foreach (string address in _cache.GetUpdatedAddresses())
            {
                if (!_cache.TryGetBan(address, out Ban? ban) || ban == null)
                {
                    _logger.LogWarning("Table Manager failed to get updated ban from the cache for address {address}.", address);
                    continue;
                }

                BanListEntry updatedEntry = new()
                {
                    PartitionKey = _banListPartitionKey,
                    RowKey = address,
                    Timestamp = DateTime.UtcNow,
                    NowBanned = ban.Type != Ban.BanType.Unbanned,
                    UnbannedOn = ban.Expiration,
                    IsLifetime = ban.Type == Ban.BanType.Life,
                    NumTempBans = ban.BanCount
                };

                if (ban.TimeStamp > _bootTime && ban.BanCount == 1)
                    _ = NewEntry(address, updatedEntry);
                else
                    _ = UpdateEntry(address, updatedEntry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table Update failed: {message}", ex.Message);
        }
    }

    private async Task<HashSet<BanListEntry>> GetRecordsAsync(Func<BanListEntry, bool>? filter)
    {
        filter ??= _ => true;

        if (string.IsNullOrEmpty(_banListPartitionKey))
        {
            _logger.LogWarning("The partition key for querying the banlist table was invalid. Cache was not populated.");
            return [];
        }
        try
        {
            List<BanListEntry> recordList = [];
            var tableEntities = _banTableClient.QueryAsync<BanListEntry>(e => e.PartitionKey == _banListPartitionKey);
            await foreach (var tableEntity in tableEntities)
                recordList.Add(tableEntity);

            var pruneList = recordList.Where(e => ShouldPrune(e));

            var filteredList = recordList
                .Except(pruneList)
                .Where(filter);

            _ = Task.Run(() => Prune([.. pruneList]));

            return [.. filteredList];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "There was an error while fetching banlist records: {message}.", ex.Message);
            return [];
        }
    }

    //private async Task<BanListEntry?> GetEntry(string ipAddress)
    //{
    //    try {
    //        var response = await _tableClient.GetEntityAsync<BanListEntry>(_partitionKey, ipAddress);
    //        return response.Value;
    //    } catch (RequestFailedException failedToFind) when (failedToFind.Status == 404) {
    //        return null;
    //    } catch (Exception ex) {
    //        _logger.LogError(ex, "Error in banlist lookup for IP {address}", ipAddress);
    //        return null;
    //    }
    //}

    private async Task<bool> NewEntry(string ipAddress, BanListEntry entry)
    {
        try
        {
            entry.PartitionKey ??= _banListPartitionKey;
            entry.RowKey ??= ipAddress;
            await _tableSemaphore.WaitAsync();
            try
            {
                var response = await _banTableClient.AddEntityAsync(entry);
            }
            finally
            {
                _tableSemaphore.Release();
            }

            _tagCache.TryAdd(entry.RowKey, entry.ETag);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when attempting to a add via table client: {message}", ex.Message);
            return false;
        }
    }

    private async Task<bool> UpdateEntry(string ipAddress, BanListEntry updatedEntry)
    {
        try
        {
            updatedEntry.PartitionKey ??= _banListPartitionKey;
            updatedEntry.RowKey ??= ipAddress;
            bool tagCached = _tagCache.TryGetValue(ipAddress, out ETag entryTag);

            await _tableSemaphore.WaitAsync();
            try
            {
                var response = await _banTableClient.UpdateEntityAsync(
                    updatedEntry,
                    tagCached ? entryTag :
                        updatedEntry.ETag != default ? updatedEntry.ETag : default,
                    TableUpdateMode.Merge);
            }
            finally
            {
                _tableSemaphore.Release();
            }

            if (!tagCached && updatedEntry.ETag == default)
            {
                _logger.LogWarning("An entry update was attempted for IP {ip} without a proper ETag.", ipAddress);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when attempting to a update via table client: {message}", ex.Message);
            return false;
        }
    }

    private async Task<bool> RemoveEntry(string ipAddress)
    {
        try
        {
            await _tableSemaphore.WaitAsync();
            try
            {
                var response = await _banTableClient.DeleteEntityAsync(_banListPartitionKey, ipAddress);
            }
            finally
            {
                _tableSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when attempting to a remove an entry via table client: {message}", ex.Message);
            return false;
        }

        return true;
    }

    private bool ShouldPrune(BanListEntry entry)
    {
        return entry switch
        {
            { IsLifetime: true } => false,
            { NowBanned: true } when DateTime.UtcNow - entry.Timestamp < _entryDuration => false,
            { NowBanned: true } when DateTime.UtcNow - entry.Timestamp > _entryDuration => true,
            { NowBanned: false } when DateTime.UtcNow - entry.Timestamp > _entryDuration => true,
            _ => false
        };
    }

    private void Prune(BanListEntry[] entries)
    {
        foreach (var entry in entries)
            _ = Task.Run(() => RemoveEntry(entry.RowKey));
    }

    private async Task<bool> ShouldPruneDataBase(IConfiguration config)
    {
        var pruneTimeStr = config["PruneDBAfterDays"] ?? "7";
        if (!int.TryParse(pruneTimeStr, out int pruneTime))
            pruneTime = 7;
        var pruneDuration = TimeSpan.FromDays(pruneTime);

        try
        {
            await _tableSemaphore.WaitAsync();
            var lastPrune = _appVarsTableClient.GetEntityIfExistsAsync
        }


        var incompleteCutOff = config["PruneIncompleteGamesAfterDays"];
    }
}
