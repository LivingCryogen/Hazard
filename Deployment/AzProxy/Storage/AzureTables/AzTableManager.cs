using AzProxy.Storage.AzureTables.AppVariables;
using AzProxy.Storage.AzureTables.BanList;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Net;
using System.Text.Json;

namespace AzProxy.Storage.AzureTables;

internal abstract class AzTableManagerBase
{
    private readonly TableClient _tableClient;
    private readonly SemaphoreSlim _tableSemaphore = new(1, 1);

    protected ILogger Logger { get; }
    protected string PartitionKey { get; }
    protected TimeSpan? EntryDuration { get; }

    public AzTableManagerBase(TableClient tableClient, ILogger logger, string partitionKey, TimeSpan? entryDuration)
    {
        _tableClient = tableClient;
        Logger = logger;
        PartitionKey = partitionKey;
        EntryDuration = entryDuration;
    }

    protected async Task<bool> AddAsync<T>(T entity) where T : class, ITableEntity
    {
        try
        {
            await _tableSemaphore.WaitAsync();
            try
            {
                var tableResponse = await _tableClient.AddEntityAsync(entity).ConfigureAwait(false);

                int statusCode = tableResponse.Status;

                if (statusCode >= 200 && statusCode < 300)
                {
                    Logger.LogInformation("Successfully added entry.");
                    return true;
                }

                Logger.LogWarning("Unexpected status {status} when adding entry.", tableResponse.Status);
                return false;
            }
            finally
            {
                _tableSemaphore.Release();
            }
        }
        catch (RequestFailedException rfEx)
        {
            Logger.LogError(rfEx, "Azure Request Failed when attempting to add entry (Status: {status}): {message}", rfEx.Status, rfEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error when persisting entry to Azure Table: {message}", ex.Message);
            return false;
        }
    }

    protected async Task<bool> UpdateAsync<T>(T entity) where T : class, ITableEntity
    {
        try
        {
            var tableResponse = await _tableClient.UpdateEntityAsync(entity,
                entity.ETag != default
                    ? entity.ETag
                    : ETag.All,
                TableUpdateMode.Replace);
            int statusCode = tableResponse.Status;

            if (statusCode >= 200 && statusCode < 300)
            {
                Logger.LogInformation("Successfully updated entry.");
                return true;
            }
            Logger.LogWarning("Unexpected status {status} when updating entry.", tableResponse.Status);
            return false;
        }
        catch (RequestFailedException rfEx)
        {
            Logger.LogError(rfEx, "Azure Request Failed when attempting to update entry (Status: {status}): {message}", rfEx.Status, rfEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unexpected error when updating entry in Azure Table: {message}", ex.Message);
            return false;
        }
    }

    protected async Task<bool> DeleteByRowKeyAsync<T>(string rowKey) where T : class, ITableEntity
    {
        try
        {
            await _tableSemaphore.WaitAsync();
            try
            {
                var response = await _tableClient.DeleteEntityAsync(PartitionKey, rowKey).ConfigureAwait(false);

                if (response.Status >= 200 && response.Status < 300)
                {
                    Logger.LogInformation("Successfully deleted entry with id: {id}.", rowKey);
                    return true;
                }

                Logger.LogWarning("Unexpected status {status} when deleting entry {id}.", response.Status, rowKey);
                return false;
            }
            finally
            {
                _tableSemaphore.Release();
            }
        }
        catch (RequestFailedException rfEx)
        {
            Logger.LogError(rfEx, "Azure Request Failed when attempting to delete entry {id} (Status: {status}): {message}", rowKey, rfEx.Status, rfEx.Message);
            return false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error when attempting to a remove an entry via table client: {message}", ex.Message);
            return false;
        }
    }

    protected IAsyncEnumerable<T> QueryByPartitionAsync<T>(Expression<Func<T, bool>> filter) where T : class, ITableEntity
        => _tableClient.QueryAsync<T>(filter);

    // Add a new App Variable entry to Azure Table storage
    public async Task AddAppVarTableEntry(AppVarEntry entry)
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

    // Create a new AppVarEntry with only default properties
    public AppVarEntry GetNewAppVarEntry()
    {
        return new AppVarEntry()
        {
            PartitionKey = _appVarsPartitionKey,
            Timestamp = DateTime.UtcNow
        };
    }
}
