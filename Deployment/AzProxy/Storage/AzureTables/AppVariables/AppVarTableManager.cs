using AzProxy.Storage.AzureTables;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzProxy.Storage.AzureTables.AppVariables;

public class AppVarTableManager : AzTableManagerBase
{
    private readonly ILogger<AppVarTableManager> _logger;
    private readonly HashSet<AppVarEntry> _variableEntries;
    private readonly string _appVarsPartitionKey;
    private readonly string _defaultAppVarsJson;

    public Dictionary<string, (string TypeName, string Value)> Variables { get; }

    internal AppVarTableManager(ILogger<AppVarTableManager> logger, string connectionString, string tableName, TimeSpan? entryDuration, string defaultJson)
        : base(new TableClient(connectionString, "BanList"), logger, tableName, null)
    {
        _defaultAppVarsJson = defaultJson;

        _variableEntries = InitializeVariables();
        Variables = CreatePublicVariablesDictionary();
    }

    public async Task<HashSet<AppVarEntry>> GetOrSetDefaultVars()
    {
        var queryResults = new List<AppVarEntry>();
        await foreach (AppVarEntry varEntity in
            _variableTable
                .QueryAsync<AppVarEntry>(e => e.PartitionKey == _appVarsPartitionKey))
            queryResults.Add(varEntity);
        if (queryResults.Count > 0)
        {
            HashSet<AppVarEntry> varEntries = [];
            foreach (var varEntry in queryResults)
                if (ValidateAppVarEntry(varEntry))
                    varEntries.Add(varEntry);
                else
                    _logger.LogWarning("Failed to validate an app variable entry with rowkey {name}, value {val}.", varEntry.RowKey, varEntry.Value);

            _logger.LogInformation("Loaded {count} App Variables from Azure Table entries.", _appVars.Count);
            return varEntries;
        }

        if (string.IsNullOrEmpty(_defaultAppVarsJson))
        {
            _logger.LogWarning("No App variables were found in either Azure Table or Azure Configuration variable. Using hard-coded defaults when possible.");
            return [];
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
            return [];
        }

        HashSet<AppVarEntry> defaultEntries = [];
        foreach (var kvp in variableCollection)
        {
            var newDefaultVarEntry = GetAppVarEntryFromJsonElement(kvp.Key, kvp.Value);

            if (ValidateAppVarEntry(newDefaultVarEntry))
            {
                defaultEntries.Add(newDefaultVarEntry);

                _logger.LogInformation("Adding default app variable entry {name} with value {val} to Azure Table storage.",
                    newDefaultVarEntry.RowKey, newDefaultVarEntry.Value);
                await AddAppVarTableEntry(newDefaultVarEntry);
            }
            else
            {
                _logger.LogWarning("Failed to validate an app variable entry with rowkey {name}, value {val}.", newDefaultVarEntry.RowKey, newDefaultVarEntry.Value);
            }
        }

        _logger.LogInformation("Loaded {count} App Variables from Configuration defaults.", defaultEntries.Count);
        return defaultEntries;
    }

    private HashSet<AppVarEntry> InitializeVariables()
    {

    }

    private Dictionary<string, (string Name, string Value)> InitializeVariablesDictionary()
    {

    }

    // Add a new App Variable entry to Azure Table storage
    public async Task AddAppVarTableEntry(AppVarEntry entry)
    {
        try
        {
            TableClient client = _variableTable.GetTableClient();
            client.AddEntityAsync
            var response = await _variableTable.AddEntityAsync(entry);
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
}
