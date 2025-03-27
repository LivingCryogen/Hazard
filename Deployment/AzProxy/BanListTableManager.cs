using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.Net;

namespace AzProxy;

public class BanListTableManager : IHostedService
{
    private readonly IHostApplicationLifetime _appLife;
    private readonly ILogger _logger;
    private readonly TableClient _tableClient;
    private readonly string _partitionKey;
    private readonly TimeSpan _entryDuration;
    private readonly ConcurrentDictionary<string, ETag> _tagCache = new(); // needed for easy updates

    public BanListTableManager(IConfiguration config, IHostApplicationLifetime appLife, ILogger logger, IBanCache cache)
    {
        _appLife = appLife;
        _logger = logger;
        Cache = cache;

        try {
            _tableClient = new TableServiceClient(config["StorageConnectionString"] ?? string.Empty).GetTableClient(config["TableName"] ?? string.Empty);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to construct TableClient due to an error: {message}", ex.Message);
        }
        if (_tableClient == null)
            _logger.LogError("Failed to construct TableClient.");
        _partitionKey = config["PartitionKey"] ?? string.Empty;
        _entryDuration = int.TryParse(config["EntryDurationDays"], out int result) ? TimeSpan.FromDays(result) : TimeSpan.FromDays(365);
    }

    public IBanCache Cache { get; private init; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _appLife.ApplicationStopping.Register(OnAppStopping);

        await PopulateCache();

        bool cacheInitialized = await _cache.Initialize(await _tableManager.GetRecordsAsync());
        if (!cacheInitialized)
            _logger.LogError()
        return Task.CompletedTask;
    }

    private async Task PopulateCache()
    {
        var recordedBans = await GetRecordsAsync((entry, isBanned) => { entry.NowBanned; })
    }

    //            _tagCache.TryAdd(entry.RowKey, entry.ETag);
    //            if (entry.NowBanned && entry.IsLifetime) {
    //                cache.PermaBanList.Add(entry.RowKey);
    //                continue;
    //            }
    //            if (entry.NowBanned) {
    //                cache.TempBanList.TryAdd(entry.RowKey, (entry.UnbannedOn, entry.NumTempBans));
    //            }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async void OnAppStopping()
    {
        foreach (string unBanned in _banBuffer.NewUnbanned) {
            var modified = new BanListEntry { RowKey = unBanned, NowBanned = false, UnbannedOn = DateTime.UtcNow }
            await _tableManager.UpdateEntry(unBanned, modified);
        }
        // new bans
    }



    public async Task<HashSet<BanListEntry>> GetRecordsAsync(Func<BanListEntry, bool>? filter) 
    {
        filter ??= _ => true; 

        if (string.IsNullOrEmpty(_partitionKey)) {
            _logger.LogWarning("The partition key for querying the banlist table was invalid. Cache was not populated.");
            return [];
        }
        try {
            HashSet<BanListEntry> records = [];
            var tableEntities = _tableClient.Query<BanListEntry>(e => e.PartitionKey == _partitionKey);

            var filteredEntities = tableEntities
                .Where(e => !ShouldPrune(e))
                .Where(filter);

            foreach (var entry in tableEntities) {
                if (ShouldPrune(entry))
                    await RemoveEntry(entry.RowKey);
            }

            foreach (var entry in filteredEntities) {
                records.Add(entry);
            }

            return records;
        } catch (Exception ex) {
            _logger.LogError(ex, "There was an error while fetching banlist records: {message}.", ex.Message);
            return [];
        }
    }

    public async Task<BanListEntry?> GetEntry(string ipAddress)
    {
        try {
            var response = await _tableClient.GetEntityAsync<BanListEntry>(_partitionKey, ipAddress);
            return response.Value;
        } catch (RequestFailedException failedToFind) when (failedToFind.Status == 404) {
            return null;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error in banlist lookup for IP {address}", ipAddress);
            return null;
        }
    }

    public async Task<bool> NewEntry(string ipAddress, BanListEntry entry)
    {
        try {
            entry.PartitionKey ??= _partitionKey;
            entry.RowKey ??= ipAddress;

            var response = await _tableClient.AddEntityAsync(entry);
            _tagCache.TryAdd(entry.RowKey, entry.ETag);
            return true;
        } catch (Exception ex) {
            _logger.LogError(ex, "Error when attempting to a add via table client: {message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> UpdateEntry(string ipAddress, BanListEntry updatedEntry)
    {
        try {
            updatedEntry.PartitionKey ??= _partitionKey;
            updatedEntry.RowKey ??= ipAddress;
            if (_tagCache.TryGetValue(ipAddress, out ETag entryTag)) {
                var response = await _tableClient.UpdateEntityAsync(updatedEntry, entryTag, TableUpdateMode.Merge);
                return true;
            }
            else if (updatedEntry.ETag != default) {
                var response = await _tableClient.UpdateEntityAsync(updatedEntry, updatedEntry.ETag, TableUpdateMode.Merge);
                return true;
            }
            else {
                _logger.LogWarning("An entry update was attempted for IP {ip} without a proper ETag.", ipAddress);
                return false;
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Error when attempting to a update via table client: {message}", ex.Message);
            return false;
        }
    }

    public async Task<bool> RemoveEntry(string ipAddress)
    {
        try {
            var response = await _tableClient.DeleteEntityAsync(_partitionKey, ipAddress);
            return true;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Error when attempting to a remove an entry via table client: {message}", ex.Message);
            return false;
        }
    }

    public bool ShouldPrune(BanListEntry entry)
    {
        return entry switch {
            { IsLifetime: true } => false,
            { NowBanned: true } when DateTime.UtcNow - entry.Timestamp < _tempBanHistoryDuration => false,
            { NowBanned: true } when DateTime.UtcNow - entry.Timestamp > _tempBanHistoryDuration => true,
            { NowBanned: false } when DateTime.UtcNow - entry.Timestamp > _tempBanHistoryDuration => true,
            _ => false
        };
    }
}
