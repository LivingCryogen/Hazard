using AzProxy.Storage.AzureTables.BanList;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AzProxy.Storage.AzureTables;

public class AzTableManager
{
    private readonly ILogger<AzTableManager> _logger;
    private readonly DateTimeOffset _bootTime = DateTimeOffset.UtcNow;
    private readonly TableClient _banTableClient;
    private readonly TableClient _appVarsTableClient;
    private readonly string _banListPartitionKey;
    private readonly string _appVarsPartitionKey;
    private readonly string _defaultAppVarsJson;
    private readonly TimeSpan _entryDuration;
    private readonly TimeSpan _pruneAfterDuration;
    private readonly TimeSpan _pruneIncompleteGamesAfterDuration;
    private readonly ConcurrentDictionary<string, ETag> _tagCache = new(); // needed for easy updates
    private readonly SemaphoreSlim _tableSemaphore = new(1, 1);

    public AzTableManager(IConfiguration config, ILogger<AzTableManager> logger)
    {
        _logger = logger;

        string? storageConnection = config["StorageConnectionString"];

        if (string.IsNullOrEmpty(storageConnection))
        {
            _logger.LogError("Azure Table access configuration incorrect.");
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
        if (!double.TryParse(config["PruneDBAfterDays"], out double pruneDays))
        {
            _logger.LogWarning("PruneDBAfterDays configuration invalid or missing; defaulting to 7 days.");
            pruneDays = 7;
        }
        else
            _pruneAfterDuration = TimeSpan.FromDays(pruneDays);
        if (!double.TryParse(config["PruneIncompleteGamesAfterDays"], out double incGamePruneDays))
        {
            _logger.LogWarning("PruneIncompleteGamesAfterDays configuration invalid or missing; defaulting to 90 days.");
            incGamePruneDays = 90;
        }
        else
            _pruneIncompleteGamesAfterDuration = TimeSpan.FromDays(incGamePruneDays);

        if (_banListPartitionKey == string.Empty)
            logger.LogWarning("Banlist partition key empty.");
        if (_appVarsPartitionKey == string.Empty)
            logger.LogWarning("AppVars partition key empty.");
        if (_defaultAppVarsJson == string.Empty)
            logger.LogWarning("Default App Variable definitions empty.");

        _entryDuration = int.TryParse(config["EntryDurationDays"], out int result) ? TimeSpan.FromDays(result) : TimeSpan.FromDays(365);
    }

    // Load App Variables from Azure Table storage, or set to defaults from configuration if none exist
    public async Task<List<AppVarEntry>> GetOrSetDefaultVars()
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
            return _appVars;
        }

        if (string.IsNullOrEmpty(_defaultAppVarsJson))
        {
            _logger.LogWarning("No App variables were found in either Azure Table or Azure Configuration variable. Using hard-coded defaults when possible.");
            return _appVars;
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
            return _appVars;
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

                await AddAppVarTableEntry(newDefaultVarEntry);
            }
            else
            {
                _logger.LogWarning("Failed to validate an app variable entry with rowkey {name}, value {val}.", newDefaultVarEntry.RowKey, newDefaultVarEntry.Value);
            }
        }

        _logger.LogInformation("Loaded {count} App Variables from Configuration defaults.", _appVars.Count);
        return _appVars;
    }

    // Add a new App Variable entry to Azure Table storage
    private async Task AddAppVarTableEntry(AppVarEntry entry)
    {
        try
        {
            var response = await _appVarsTableClient.AddEntityAsync(entry);
            if (response.Status == 204)
                _logger.LogInformation("Successfully added App Variable entry {name}.", entry.RowKey);
            else
                _logger.LogWarning("Unexpected status {status} when adding App Variable entry {name}.", response.Status, entry.RowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to persist App Variable entry {name} to Azure Table: {message}", entry.RowKey, ex.Message);
        }
    }

    // Update an existing App Variable entry in Azure Table storage
    public async Task UpdateAppVarTableEntry(AppVarEntry entry)
    {
        try
        {
            var response = await _appVarsTableClient.UpdateEntityAsync(entry,
                entry.ETag != default
                    ? entry.ETag
                    : ETag.All,
                TableUpdateMode.Replace);
            if (response.Status == 204)
                _logger.LogInformation("Successfully updated App Variable entry {name}.", entry.RowKey);
            else
                _logger.LogWarning("Unexpected status {status} when adding App Variable entry {name}.", response.Status, entry.RowKey);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to persist App Variable entry {name} to Azure Table: {message}", entry.RowKey, ex.Message);
        }
    }

    // Construct an AppVarEntry from a JsonElement definition
    private AppVarEntry GetAppVarEntryFromJsonElement(string varNameAndKey, JsonElement jsonElement)
    {
        return new()
        {
            PartitionKey = _appVarsPartitionKey,
            RowKey = varNameAndKey,
            TypeName = jsonElement.GetProperty("Type").GetString() ?? throw new InvalidDataException(),
            Description = jsonElement.GetProperty("Description").GetString() ?? string.Empty,
            Timestamp = DateTimeOffset.UtcNow,
            Value = jsonElement.GetProperty("Value").GetString() ?? ""
        };
    }

    // Validate that an App Variable entry's value matches its declared type
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
                                && !stringValues.Any(str => string.IsNullOrEmpty(str)),
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

    // Fetch banlist records from Azure Table storage, applying an optional filter
    public async Task<HashSet<BanListEntry>> GetRecordsAsync(Func<BanListEntry, bool>? filter)
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
    // Add a new banlist entry to Azure Table storage
    private async Task<bool> NewEntry(string ipAddress, BanListEntry entry)
    {
        try
        {
            entry.PartitionKey ??= _banListPartitionKey;
            entry.RowKey ??= ipAddress;
            await _tableSemaphore.WaitAsync();
            try
            {
                var response = await _banTableClient.AddEntityAsync(entry).ConfigureAwait(false);
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

    // Update an existing banlist entry in Azure Table storage
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
                    TableUpdateMode.Merge).ConfigureAwait(false);
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

    // Remove a banlist entry from Azure Table storage
    private async Task<bool> RemoveEntry(string ipAddress)
    {
        try
        {
            await _tableSemaphore.WaitAsync();
            try
            {
                var response = await _banTableClient.DeleteEntityAsync(_banListPartitionKey, ipAddress).ConfigureAwait(false);
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

    // Determine if a banlist entry should be pruned based on its age and ban status
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

    // Prune a collection of banlist entries from Azure Table storage
    private async Task Prune(BanListEntry[] entries)
    {
        foreach (var entry in entries)
            await RemoveEntry(entry.RowKey);
    }

    public async Task PersistBan(string address, Ban sessionBan)
    {
        BanListEntry updatedEntry = new()
        {
            PartitionKey = _banListPartitionKey,
            RowKey = address,
            Timestamp = DateTime.UtcNow,
            NowBanned = sessionBan.Type != Ban.BanType.Unbanned,
            UnbannedOn = sessionBan.Expiration,
            IsLifetime = sessionBan.Type == Ban.BanType.Life,
            NumTempBans = sessionBan.BanCount
        };

        if (sessionBan.TimeStamp > _bootTime && sessionBan.BanCount == 1)
            _ = await NewEntry(address, updatedEntry);
        else
            _ = await UpdateEntry(address, updatedEntry);
    }
}
