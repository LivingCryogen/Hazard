using Azure;
using Azure.Data.Tables;
using System.Collections.Concurrent;

namespace AzProxy;

public class BanListTableManager : IHostedService
{
    private readonly IHostApplicationLifetime _appLife;
    private readonly DateTimeOffset _bootTime = DateTimeOffset.UtcNow;
    private readonly ILogger _logger;
    private readonly IBanCache _cache;
    private readonly TableClient _tableClient;
    private readonly string _partitionKey;
    private readonly TimeSpan _entryDuration;
    private readonly ConcurrentDictionary<string, ETag> _tagCache = new(); // needed for easy updates
    private readonly SemaphoreSlim _tableSemaphore = new(1, 1);

    public BanListTableManager(IConfiguration config, IHostApplicationLifetime appLife, ILogger logger, IBanCache cache)
    {
        _appLife = appLife;
        _logger = logger;
        _cache = cache;

        try {
            _tableClient = new TableServiceClient(config["StorageConnectionString"] ?? string.Empty).GetTableClient(config["TableName"] ?? string.Empty);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to construct TableClient due to an error: {message}", ex.Message);
        }
        if (_tableClient == null) {
            throw new NullReferenceException("Failed to construct TableClient.");
        }
        
        _partitionKey = config["PartitionKey"] ?? string.Empty;
        _entryDuration = int.TryParse(config["EntryDurationDays"], out int result) ? TimeSpan.FromDays(result) : TimeSpan.FromDays(365);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _appLife.ApplicationStopping.Register(OnAppStopping);

        await PopulateCache();
            
        return;
    }

    private async Task PopulateCache()
    {
        var recordedBans = await GetRecordsAsync((entry) => entry.NowBanned);
        _cache.Initialize(recordedBans);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void OnAppStopping()
    {
        List<Ban> updatedBans = [];
        _logger.LogInformation("Beginning Table Update....");

        try {
            foreach (string address in _cache.GetUpdatedAddresses()) {
                if (!_cache.TryGetBan(address, out Ban? ban) || ban == null) {
                    _logger.LogWarning("Table Manager failed to get updated ban from the cache for address {address}.", address);
                    continue;
                }

                BanListEntry updatedEntry = new() {
                    PartitionKey = _partitionKey,
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
        } catch (Exception ex) {
            _logger.LogError(ex, "Table Update failed: {message}", ex.Message);
        }
    }

    private async Task<HashSet<BanListEntry>> GetRecordsAsync(Func<BanListEntry, bool>? filter) 
    {
        filter ??= _ => true; 

        if (string.IsNullOrEmpty(_partitionKey)) {
            _logger.LogWarning("The partition key for querying the banlist table was invalid. Cache was not populated.");
            return [];
        }
        try {
            List<BanListEntry> recordList = [];
            var tableEntities = _tableClient.QueryAsync<BanListEntry>(e => e.PartitionKey == _partitionKey);
            await foreach (var tableEntity in tableEntities)
                recordList.Add(tableEntity);

            var pruneList = recordList.Where(e => ShouldPrune(e));

            var filteredList = recordList
                .Except(pruneList)
                .Where(filter);

            _ = Task.Run(() => Prune([.. pruneList]));

            return [.. filteredList];
        } catch (Exception ex) {
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
        try {
            entry.PartitionKey ??= _partitionKey;
            entry.RowKey ??= ipAddress;
            await _tableSemaphore.WaitAsync();
            try {
                var response = await _tableClient.AddEntityAsync(entry);
            }
            finally {
                _tableSemaphore.Release();
            }
            
            _tagCache.TryAdd(entry.RowKey, entry.ETag);
            return true;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error when attempting to a add via table client: {message}", ex.Message);
            return false;
        }
    }

    private async Task<bool> UpdateEntry(string ipAddress, BanListEntry updatedEntry)
    {
        try {
            updatedEntry.PartitionKey ??= _partitionKey;
            updatedEntry.RowKey ??= ipAddress;
            bool tagCached = _tagCache.TryGetValue(ipAddress, out ETag entryTag);

            await _tableSemaphore.WaitAsync();
            try {
                var response = await _tableClient.UpdateEntityAsync(
                    updatedEntry,
                    tagCached ? entryTag :
                        updatedEntry.ETag != default ? updatedEntry.ETag : default,
                    TableUpdateMode.Merge);
            }
            finally {
                _tableSemaphore.Release();
            }

            if (!tagCached && updatedEntry.ETag == default) { 
                _logger.LogWarning("An entry update was attempted for IP {ip} without a proper ETag.", ipAddress);
                return false;
            }
            return true;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error when attempting to a update via table client: {message}", ex.Message);
            return false;
        }
    }

    private async Task<bool> RemoveEntry(string ipAddress)
    {
        try {
            await _tableSemaphore.WaitAsync();
            try {
                var response = await _tableClient.DeleteEntityAsync(_partitionKey, ipAddress);
            }
            finally {
                _tableSemaphore.Release();
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error when attempting to a remove an entry via table client: {message}", ex.Message);
            return false;
        }

        return true;
    }

    private bool ShouldPrune(BanListEntry entry)
    {
        return entry switch {
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
}
