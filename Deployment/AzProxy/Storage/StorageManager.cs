using AzProxy.Requests;
using AzProxy.Storage.AzureDB;
using AzProxy.Storage.AzureDB.Context;
using AzProxy.Storage.AzureDB.Entities;
using AzProxy.Storage.AzureTables;
using AzProxy.Storage.AzureTables.AppVariables;
using AzProxy.Storage.AzureTables.BanList;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace AzProxy.Storage;

public class StorageManager : IHostedService
{
    private record LastPruneDateResult(bool IsValid, AppVarEntry? Entry);

    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _appLife;
    private readonly ILogger _logger;
    private readonly IBanCache _cache;
    private readonly BanListTableManager _banListManager;
    private readonly AppVarTableManager _appVarManager;
    private readonly AzDBManager _azDBManager;


    private HashSet<AppVarEntry> _appVarSet;

    private PruneRequest? _defaultPruneRequest;
    private LastPruneDateResult _dBLastPrunedFetchResult = new(false, null);

    public StorageManager(IConfiguration config, 
        IHostApplicationLifetime appLife, 
        ILoggerFactory loggerFactory, 
        IBanCache cache, 
        IServiceProvider serviceProvider, 
        AzDBManager azDBManager)
    {
        _appLife = appLife;
        _logger = loggerFactory.CreateLogger<StorageManager>();
        _cache = cache;
        _serviceProvider = serviceProvider;
        _azDBManager = azDBManager;

        string? storageConnection = config["StorageConnectionString"];

        if (string.IsNullOrEmpty(storageConnection))
        {
            _logger.LogError("Azure Table access configuration incorrect.");
            throw new NullReferenceException();
        }

        string banTableName = config["BanTableName"] ?? string.Empty;
        string varsTableName = config["VariablesTableName"] ?? string.Empty;
        string banListPartitionKey = config["BanlistPartitionKey"] ?? string.Empty;
        string appVarsPartitionKey = config["AppVarsPartitionKey"] ?? string.Empty;
        string defaultAppVarsJson = config["AppVarsJSONDefinitions"] ?? string.Empty;
        string banListDuration = config["EntryDurationDays"] ?? string.Empty;
        TimeSpan banListEntryDuration;

        if (banTableName == string.Empty)
            _logger.LogWarning("Ban table name empty.");
        if (banListPartitionKey == string.Empty)
            _logger.LogWarning("Banlist partition key empty.");
        if (appVarsPartitionKey == string.Empty)
            _logger.LogWarning("AppVars partition key empty.");
        if (defaultAppVarsJson == string.Empty)
            _logger.LogWarning("Default App Variable definitions empty.");
        if (int.TryParse(banListDuration, out int result))
            banListEntryDuration = TimeSpan.FromDays(result);
        else
        {
            _logger.LogWarning("Banlist entry duration configuration invalid or missing; defaulting to 365 days.");
            banListEntryDuration = TimeSpan.FromDays(365);
        }
        
        _banListManager = new(loggerFactory.CreateLogger<BanListTableManager>(), storageConnection, banTableName, banListEntryDuration);

        _appVarManager = new(loggerFactory.CreateLogger<AppVarTableManager>(), storageConnection, varsTableName, null, defaultAppVarsJson);

        if (_appVarManager == null)
            _logger.LogError("AppVariableManager failed to initialize.");
        if (_banListManager == null)
            _logger.LogError("BanListTableManager failed to initialize.");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await PopulateCache(_banListManager);
        if (!await InitializeAppVariables()) 
            return;

        // Fetch the LastDBPruneDate App Var entry and validate, storing result for use on shutdown
        // Then, set the AzDBManager's last prune date from the entry value if valid

        var lastPruneVar = _appVarSet.Where(v => v.RowKey == "LastDBPruneDate").FirstOrDefault();
        if (lastPruneVar?.TypeName != typeof(DateTime).Name || !DateTime.TryParse(lastPruneVar.Value, out DateTime lastPruneDate))
        {
            _logger.LogWarning("No valid LastDBPruneDate app variable Entry found on startup. Expected Type name of DateTime and value compatible with DateTime.");
            _dBLastPrunedFetchResult = new LastPruneDateResult(false, null);
            return;
        }

        if (lastPruneDate == default || lastPruneDate > DateTime.UtcNow)
        {
            _logger.LogWarning("No valid LastDBPruneDate app variable Entry found on startup: invalid DateTime value.");
            _dBLastPrunedFetchResult = new LastPruneDateResult(false, null);
            return;
        }

        _dBLastPrunedFetchResult = new LastPruneDateResult(true, lastPruneVar);
        _azDBManager.InitializeLastPruneDate(lastPruneDate);

        // Create default prune request for use on shutdown
        if (_azDBManager.PruneAfterDays == null)
        {
            _logger.LogWarning("AzDBManager PruneAfterDays is null; cannot create default prune request.");
            return;
        }
        _defaultPruneRequest = new(false, false, _azDBManager.PruneAfterDays);

        return;
    }

    // Note: This is reliably called in a low-traffic, cold-start scenario (like Azure Free Tier Web App) - if moved to always-on, a background service must be implemented instead.
    public async Task StopAsync(CancellationToken cancellationToken) => await OnAppStopping();

    // Initialize the in-memory cache from the Azure Table storage
    private async Task PopulateCache(BanListTableManager banListManager)
    {
        try
        {
            var recordedBans = await banListManager.GetRecordsAsync((entry) => entry.NowBanned);
            _cache.Initialize(recordedBans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while populating the ban cache: {message}", ex.Message);
        }   
    }

    // On application stopping, persist any updated bans to Azure Table storage,
    // and prune database if needed
    private async Task OnAppStopping()
    {
        await UpdateBanlist();
        // fetch laste prune date from App Vars; if no valid one is found, prune DB and set PruneDate App Var
        _logger.LogInformation("Checking if the Az Database should be pruned....");

        if (_defaultPruneRequest == null)
        {
            _logger.LogWarning("Default Prune Request is null; skipping database prune on shutdown.");
            return;
        }

        // Run default prune attempt (configuration-based/scheduled)
        await TryDBPruneAsync(_defaultPruneRequest);
        // We check to Update LastPruneDate Table variable regardless, since manual prune could have occurred during runtime.
        await UpdateLastPruneDateEntry();
    }

    // Attempt to prune the database if needed based on the LastPruneDate App Var Result and AzDBManager's pruning conditions
    // If the Prune was completed successfully, returns true; otherwise, false.
    public async Task<bool> TryDBPruneAsync(PruneRequest pruneRequest)
    {
        bool missingLastPrune = _dBLastPrunedFetchResult.Entry == null;
        bool invalidLastPrune = _dBLastPrunedFetchResult.IsValid == false;
        bool scheduledPrune = _azDBManager.ShouldPrune();
        bool forcedPrune = pruneRequest.ForcePrune;

        bool mustPrune =
            forcedPrune ||
            missingLastPrune ||
            invalidLastPrune ||
            scheduledPrune;

        if (mustPrune)
        {
            bool pruneSuccess = await _azDBManager.PruneAsync(pruneRequest);
            if (pruneSuccess)
            {                
                if (!await UpdateLastPruneDateEntry())
                    _logger.LogWarning("Prune was successful, but the LastDBPruneDate App Variable Entry was not updated to reflect this.");
                else
                    _logger.LogInformation("LastDBPruneDate AzTable Entry successfully updated.");
            }

            return pruneSuccess;
        }

        return false;
    }

    public async Task<bool> DBPrune(PruneRequest pruneRequest) => await _azDBManager.PruneAsync(pruneRequest); // THIS FORCES PRUNE

    // Update the banlist in Azure Table storage with any updated bans from the in-memory cache
    private async Task UpdateBanlist()
    {
        _logger.LogInformation("Beginning Table Banlist Update....");

        try
        {
            foreach (string address in _cache.GetUpdatedAddresses())
            {
                if (!_cache.TryGetBan(address, out Ban? ban) || ban == null)
                {
                    _logger.LogWarning("Table Manager failed to get updated ban from the cache for address {address}.", address);
                    continue;
                }

                await _banListManager.PersistBan(address, ban);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table Update failed: {message}", ex.Message);
        }
    }

    // Updates DBLastPruneDate AppVar in AzTable storage; should be called only after a successful DB prune.
    // If update is successful, returns true; otherwise, false.
    // Leverages AppVar fetch result from startup to decide whether to add new entry or update existing one.
    private async Task<bool> UpdateLastPruneDateEntry()
    {
        AppVarEntry updatedEntry;
        bool fetchedEntry = _dBLastPrunedFetchResult.Entry != null;
        bool hasPruneDate = _azDBManager.LastPruned != null;
        bool wasPruned = _azDBManager.Pruned;

        if (wasPruned && !hasPruneDate)
        {
            _logger.LogError("Unexpected mismatch between Pruned flag and PruneDate value. Canceling variable update.");
            return false;
        }

        if (!wasPruned)
        {
            _logger.LogInformation("No database prune has occurred since last fetch; skipping LastDBPruneDate entry update.");
            return false;
        }

        if (fetchedEntry)
            updatedEntry = _dBLastPrunedFetchResult.Entry!;
        else
        {
            if (!hasPruneDate)
            {
                _logger.LogError("AzDBManager's LastPruned date is null; cannot create new LastDBPruneDate entry.");
                return false;
            }

            updatedEntry = _appVarManager.MakeNewAppVarEntry();
        }

        MakeLastPruneDateEntry(updatedEntry, (DateTime)_azDBManager.LastPruned!);

        try
        {
            if (fetchedEntry)
            {
                _logger.LogInformation("Updating existing LastDBPruneDate entry in Azure Table storage, DateTime value: {val}.", updatedEntry.Value);
                await _appVarManager.UpdateAppVarTableEntry(updatedEntry);
            }
            else
            {
                _logger.LogInformation("Adding new LastDBPruneDate entry in Azure Table storage, DateTime value: {val}.", updatedEntry.Value);
                await _appVarManager.AddAppVarTableEntry(updatedEntry);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("An exception occurred when attempting to add or update LastDBPruneDate entry: {msg}", ex.Message);
            _logger.LogTrace("Error stack trace: {trace}", ex.StackTrace);
            return false;
        }
    }

    private void MakeLastPruneDateEntry(AppVarEntry varEntry, DateTime dateTime)
    {
        varEntry.RowKey = "LastDBPruneDate";
        varEntry.TypeName = "DateTime";
        varEntry.Description = "The last date the database was pruned of old entries.";
        varEntry.Value = dateTime.ToString("o");
    }

    // Load App Variables from Azure Table storage, or set to defaults from configuration if none exist
    private async Task<bool> InitializeAppVariables()
    {
        _appVarSet = await _appVarManager.GetOrSetDefaultVars();

        if (_appVarSet == null)
        {
            _logger.LogError("Failed to initialize application variables from Azure Table storage.");
            return false;
        }

        return true;
    }
}
