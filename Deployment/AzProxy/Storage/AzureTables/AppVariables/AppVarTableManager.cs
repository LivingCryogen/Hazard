using AzProxy.Storage.AzureTables;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AzProxy.Storage.AzureTables.AppVariables;

internal class AppVarTableManager : AzTableManagerBase
{
    private readonly string _defaultAppVarsJson;

    internal AppVarTableManager(ILogger logger, string connectionString, string tableName, TimeSpan? entryDuration, string defaultJson)
        : base(new TableClient(connectionString, "BanList"), logger, tableName, null)
    {
        _defaultAppVarsJson = defaultJson;
    }

    // Create a new AppVarEntry with only default properties
    public AppVarEntry MakeNewAppVarEntry()
    {
        return new AppVarEntry()
        {
            PartitionKey = PartitionKey,
            Timestamp = DateTime.UtcNow
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
            Logger.LogError("An error occured when attempting to validate app variable {name}: {message}.", entry.RowKey, ex.Message);
            Logger.LogWarning("Failed to validate app variable entry {name} of type {typename} and value {val}. This variable will be ignored.", entry.RowKey, entry.TypeName, entry.Value);
            return false;
        }
    }

    // Construct an AppVarEntry from a JsonElement definition
    private AppVarEntry GetAppVarEntryFromJsonElement(string varNameAndKey, JsonElement jsonElement)
    {
        return new()
        {
            PartitionKey = PartitionKey,
            RowKey = varNameAndKey,
            TypeName = jsonElement.GetProperty("Type").GetString() ?? throw new InvalidDataException(),
            Description = jsonElement.GetProperty("Description").GetString() ?? string.Empty,
            Timestamp = DateTimeOffset.UtcNow,
            Value = jsonElement.GetProperty("Value").GetString() ?? ""
        };
    }

    // Add a new App Variable entry to Azure Table storage
    public async Task AddAppVarTableEntry(AppVarEntry entry)
    {
        try
        {
            var added = await AddAsync(entry);
            
            if (added)
                Logger.LogInformation("Successfully added App Variable entry {name}.", entry.RowKey);
            else
                Logger.LogWarning("Unexpected failure when adding App Variable entry {name}.", entry.RowKey);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to persist App Variable entry {name} to Azure Table: {message}", entry.RowKey, ex.Message);
        }
    }

    // Update an existing App Variable entry in Azure Table storage
    public async Task UpdateAppVarTableEntry(AppVarEntry entry)
    {
        try
        {
            var updated = await UpdateAsync(entry);
            if (updated)
                Logger.LogInformation("Successfully updated App Variable entry {name}.", entry.RowKey);
            else
                Logger.LogWarning("Unexpected failure when updating App Variable entry {name}.", entry.RowKey);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to persist App Variable entry {name} to Azure Table: {message}", entry.RowKey, ex.Message);
        }
    }

    public async Task<HashSet<AppVarEntry>> GetOrSetDefaultVars()
    {
        // Fetch all existing App Variable entries from Azure Table storage. If any exist, validate and return them.
        // If none exist, attempt to set defaults from configuration or hard-coded values.
        var queryResults = new List<AppVarEntry>();
        await foreach (AppVarEntry varEntity in QueryByPartitionAsync<AppVarEntry>(e => e.PartitionKey == PartitionKey))
            queryResults.Add(varEntity);
        if (queryResults.Count > 0)
        {
            HashSet<AppVarEntry> varEntries = [];
            foreach (var varEntry in queryResults)
                if (ValidateAppVarEntry(varEntry))
                    varEntries.Add(varEntry);
                else
                    Logger.LogWarning("Failed to validate an app variable entry with rowkey {name}, value {val}.", varEntry.RowKey, varEntry.Value);

            Logger.LogInformation("Fetched {count} App Variables from Azure Table entries.", varEntries.Count);
            return varEntries;
        }

        if (string.IsNullOrEmpty(_defaultAppVarsJson))
        {
            Logger.LogWarning("No App variables were found in either Azure Table or Azure Configuration variable. Using hard-coded defaults when possible.");
            return [];
        }

        // Set App Variables to defaults from configuration JSON.
        // The JSON should be in the format of a dictionary where the key is the variable name/rowkey and the value is an object with properties Type, Value, and Description.
        // Example: { "Variable1": { "Type": "int", "Value": "5", "Description": "An example variable." },
        // "Variable2": { "Type": "string[]", "Value": "[\"a\",\"b\",\"c\"]", "Description": "Another example variable."} }
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
            Logger.LogWarning("Unexpected error when deserializing default JSON App Variable definitions: {message}. Using hard-coded defaults when possible.", ex.Message);
            return [];
        }

        // Iterate through the JSON-defined default variables, validate them, add them to Azure Table storage, and return the valid entries.
        HashSet<AppVarEntry> defaultEntries = [];
        foreach (var kvp in variableCollection)
        {
            var newDefaultVarEntry = GetAppVarEntryFromJsonElement(kvp.Key, kvp.Value);

            if (ValidateAppVarEntry(newDefaultVarEntry))
            {
                defaultEntries.Add(newDefaultVarEntry);

                Logger.LogInformation("Adding default app variable entry {name} with value {val} to Azure Table storage.",
                    newDefaultVarEntry.RowKey, newDefaultVarEntry.Value);
                await AddAppVarTableEntry(newDefaultVarEntry);
            }
            else
            {
                Logger.LogWarning("Failed to validate an app variable entry with rowkey {name}, value {val}.", newDefaultVarEntry.RowKey, newDefaultVarEntry.Value);
            }
        }

        Logger.LogInformation("Loaded {count} App Variables from Configuration defaults.", defaultEntries.Count);
        return defaultEntries;
    }
}
