using AzProxy.Storage.AzureTables;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace AzProxy.Storage;

public class AppVariableManager
{
    private readonly ILogger<AppVariableManager> _logger;
    private readonly AzTableManager _variableTable;
    private readonly HashSet<AppVarEntry> _variableEntries;
    private readonly string _appVarsPartitionKey;
    private readonly string _defaultAppVarsJson;

    public Dictionary<string, (string TypeName, string Value)> Variables { get; }

    public AppVariableManager(ILoggerFactory loggerFactory, IConfiguration config)
    {
        _logger = loggerFactory.CreateLogger<AppVariableManager>();

        string? storageConnection = config["StorageConnectionString"];

        if (string.IsNullOrEmpty(storageConnection))
        {
            _logger.LogError("Azure Table access configuration incorrect.");
            throw new NullReferenceException();
        }

        string? varsTableName = config["VariablesTableName"];
        if (string.IsNullOrEmpty(varsTableName))
            throw new ArgumentException("VarsTableName was null or empty. Check configuration (App settings).");

        _variableTable = new(loggerFactory.CreateLogger<AzTableManager>(), storageConnection, varsTableName);

        _appVarsPartitionKey = config["AppVarsPartitionKey"] ?? string.Empty;
        _defaultAppVarsJson = config["AppVarsJSONDefinitions"] ?? string.Empty;

        if (_defaultAppVarsJson == string.Empty)
            _logger.LogWarning("Default App Variable definitions empty.");
        if (_appVarsPartitionKey == string.Empty)
            _logger.LogWarning("AppVars partition key empty.");

        _variableEntries = InitializeVariables();
        Variables = CreatePublicVariablesDictionary();
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
