using AzProxy.Requests;
using AzProxy.Storage.AzureDB;
using AzProxy.Storage.AzureDB.Context;
using AzProxy.Storage.AzureDB.Entities;
using AzProxy.Storage.AzureTables;
using AzProxy.Storage.AzureTables.BanList;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AzProxy.Storage;

public class StorageManager : IHostedService
{
    private record LastPruneDateResult(bool IsValid, AppVarEntry? Entry);

    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _appLife;
    private readonly ILogger _logger;
    private readonly IBanCache _cache;
    private readonly AzTableManager _azTableManager;
    private readonly AzDBManager _azDBManager;
    private HashSet<AppVarEntry> _appVars;
    private PruneRequest? _defaultPruneRequest;
    private LastPruneDateResult _dBLastPrunedFetchResult = new(false, null);
    
    public StorageManager(IConfiguration config, 
        IHostApplicationLifetime appLife, 
        ILogger<StorageManager> logger, 
        IBanCache cache, 
        IServiceProvider serviceProvider, 
        AzTableManager azTableManager,
        AzDBManager azDBManager)
    {
        _appLife = appLife;
        _logger = logger;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _azTableManager = azTableManager;
        _azDBManager = azDBManager;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await PopulateCache(_azTableManager);
        _appVars = await GetOrSetDefaultVars();

        // Fetch the LastDBPruneDate App Var entry and validate, storing result for use on shutdown
        // Then, set the AzDBManager's last prune date from the entry value if valid

        _lastPruneDateVar = _appVars.FirstOrDefault(entry => entry.RowKey == "LastDBPruneDate");

        if (_lastPruneDateVar == null)
        {
            _logger.LogWarning("No valid LastDBPruneDate app variable Entry found on startup.");
            _dBLastPrunedFetchResult = new LastPruneDateResult(false, null);
            return;
        }

        bool validPruneDate = ValidatePruneDateEntry(_lastPruneDateVar);

        if (!validPruneDate)
        {
            _logger.LogWarning("Invalid LastDBPruneDate app variable Entry value found on startup.");
            _dBLastPrunedFetchResult = new LastPruneDateResult(false, _lastPruneDateVar);
            return;
        }

        _dBLastPrunedFetchResult = new LastPruneDateResult(validPruneDate, _lastPruneDateVar);
        _azDBManager.InitializeLastPruneDate(_lastPruneDateVar.Value);

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
    private async Task PopulateCache(AzTableManager azTableManager)
    {
        try
        {
            var recordedBans = await azTableManager.GetRecordsAsync((entry) => entry.NowBanned);
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

    // Validate the LastPruneDate App Var entry value (has a valid DateTime and is in the past)
    private static bool ValidatePruneDateEntry(AppVarEntry entry) =>
        DateTime.TryParse(entry.Value, out DateTime parsedDate) && parsedDate < DateTime.UtcNow;

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

                await _azTableManager.PersistBan(address, ban);
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

            updatedEntry = _azTableManager.GetNewAppVarEntry();
        }

        MakeLastPruneDateEntry(updatedEntry, (DateTime)_azDBManager.LastPruned!);

        try
        {
            if (fetchedEntry)
            {
                _logger.LogInformation("Updating existing LastDBPruneDate entry in Azure Table storage, DateTime value: {val}.", updatedEntry.Value);
                await _azTableManager.UpdateAppVarTableEntry(updatedEntry);
            }
            else
            {
                _logger.LogInformation("Adding new LastDBPruneDate entry in Azure Table storage, DateTime value: {val}.", updatedEntry.Value);
                await _azTableManager.AddAppVarTableEntry(updatedEntry);
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
    private async Task<HashSet<AppVarEntry>> GetOrSetDefaultVars()
    {

    }
}
