using AzProxy.BanList;
using Azure;
using Azure.Data.Tables;
using Microsoft.EntityFrameworkCore.Storage.Json;
using System.Collections.Concurrent;
using System.Text.Json;

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
    private readonly string _defaultAppVarsJson;
    private readonly TimeSpan _entryDuration;
    private readonly ConcurrentDictionary<string, ETag> _tagCache = new(); // needed for easy updates
    private readonly SemaphoreSlim _tableSemaphore = new(1, 1);
    private readonly List<AppVarEntry> _appVars = [];

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
        _defaultAppVarsJson = config["AppVarsJSONDefinitions"] ?? string.Empty;
        
        if (_banListPartitionKey == string.Empty)
            logger.LogWarning("Banlist partition key empty.");
        if (_appVarsPartitionKey == string.Empty)
            logger.LogWarning("AppVars partition key empty.");
        if (_defaultAppVarsJson == string.Empty)
            logger.LogWarning("Default App Variable definitions empty.");
        
        _entryDuration = int.TryParse(config["EntryDurationDays"], out int result) ? TimeSpan.FromDays(result) : TimeSpan.FromDays(365);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _appLife.ApplicationStopping.Register(OnAppStopping);

        await PopulateCache();
        await GetOrSetDefaultVars();

        return;
    }

    private async Task PopulateCache()
    {
        var recordedBans = await GetRecordsAsync((entry) => entry.NowBanned);
        _cache.Initialize(recordedBans);
    }

    private async Task GetOrSetDefaultVars()
    {
        var queryResults = new List<AppVarEntry>();
        await foreach (AppVarEntry varEntity in
            _appVarsTableClient
                .QueryAsync<AppVarEntry>(e => e.PartitionKey == _appVarsPartitionKey))
            queryResults.Add(varEntity);
        if (queryResults.Count > 0)
        {
            foreach (var varEntry in queryResults)
                if (ValidateAppVarEntry(varEntry))
                    _appVars.Add(varEntry);
                else
                    _logger.LogWarning("Failed to validate an app variable entry with rowkey {name}, value {val}.", varEntry.RowKey, varEntry.Value);

            _logger.LogInformation("Loaded {count} App Variables from Azure Table entries.", _appVars.Count);
            return;
        }

        if (string.IsNullOrEmpty(_defaultAppVarsJson))
        {
            _logger.LogWarning("No App variables were found in either Azure Table or Azure Configuration variable. Using hard-coded defaults when possible.");
            return;
        }

        // SET TO DEFAULT FROM CONFIG
        Dictionary<string, JsonElement>? variableCollection;
        try
        {
            variableCollection = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(_defaultAppVarsJson);
            if (variableCollection == null)
            {
                throw new InvalidDataException($"Json deserialized variable collection was null. Json : {_defaultAppVarsJson}.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Unexpected error when deserializing default JSON App Variable definitions: {message}. Using hard-coded defaults when possible.", ex.Message);
            return;
        }

        foreach (var kvp in variableCollection)
        {
            if (_appVars.Any(entry => entry.RowKey == kvp.Key))
            {
                _logger.LogWarning("A variable with duplicate rowkey/name {name} was found; ignoring...", kvp.Key);
                continue;
            }

            var newDefaultVarEntry = GetAppVarEntryFromJsonElement(kvp.Key, kvp.Value);

            if (ValidateAppVarEntry(newDefaultVarEntry))
            {
                _appVars.Add(newDefaultVarEntry);

                try
                {
                    var tableResponse = await _appVarsTableClient.AddEntityAsync(newDefaultVarEntry);
                    if (tableResponse.Status == 204)
                        _logger.LogInformation("Successfully added entry {name}.", newDefaultVarEntry.RowKey);
                    else
                        _logger.LogWarning("Unexpected status {status} when adding entry {name}.", tableResponse.Status, newDefaultVarEntry.RowKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to persist entry {name} to Azure Table: {message}", newDefaultVarEntry.RowKey, ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("Failed to validate an app variable entry with rowkey {name}, value {val}.", newDefaultVarEntry.RowKey, newDefaultVarEntry.Value);
            }
        }

        _logger.LogInformation("Loaded {count} App Variables from Configuration defaults.", _appVars.Count);
    }

    private AppVarEntry GetAppVarEntryFromJsonElement(string varNameAndKey, JsonElement jsonElement)
    {
        return new()
        {
            PartitionKey = _appVarsPartitionKey,
            RowKey = varNameAndKey,
            TypeName = jsonElement.GetProperty("Type").GetString() ?? throw new InvalidDataException(),
            Description = jsonElement.GetProperty("Description").GetString() ?? string.Empty,
            Timestamp = DateTime.UtcNow,
            Value = jsonElement.GetProperty("Value").GetString() ?? throw new InvalidDataException()
        };
    }

    // App Vars do NOT support nested objects or custom Types!
    private bool ValidateAppVarEntry(AppVarEntry entry)
    {
        string typeName = entry.TypeName.Trim().ToLowerInvariant();
        try
        {
            return typeName switch
            {
                "int" => int.TryParse(entry.Value, out _),
                "string" => !string.IsNullOrEmpty(entry.Value),
                "bool" => bool.TryParse(entry.Value, out _),
                "datetime" => DateTime.TryParse(entry.Value, out _),
                "double" => double.TryParse(entry.Value, out _),
                "string[]" => JsonSerializer.Deserialize<string[]>(entry.Value) is string[] stringValues
                                && (!stringValues.Any(str => string.IsNullOrEmpty(str))),
                "int[]" => JsonSerializer.Deserialize<int[]>(entry.Value) is int[] intValues
                            && intValues.Length != 0,
                _ => false,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occured when attempting to validate app variable {name}: {message}.", entry.RowKey, ex.Message);
            _logger.LogWarning("Failed to validate app variable entry {name} of type {typename} and value {val}. This variable will be ignored.", entry.RowKey, entry.TypeName, entry.Value);
            return false;
        }
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

        try
        {
            if (ShouldPruneDataBase())
        }
        catch (Exception ex)
        {

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
