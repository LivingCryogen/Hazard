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
    private LastPruneDateResult _dBLastPrunedResult = new(false, null);
    

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
        _appVars = await _azTableManager.GetOrSetDefaultVars();

        // Fetch the LastDBPruneDate App Var entry and validate, storing result for use on shutdown
        // Then, set the AzDBManager's last prune date from the entry value if valid

        var prunedEntry = _appVars.FirstOrDefault(entry => entry.RowKey == "LastDBPruneDate");

        if (prunedEntry == null)
        {
            _logger.LogWarning("No valid LastDBPruneDate app variable Entry found on startup.");
            _dBLastPrunedResult = new LastPruneDateResult(false, null);
            return;
        }

        bool validPruneDate = ValidatePruneDateEntry(prunedEntry);

        if (!validPruneDate)
        {
            _logger.LogWarning("Invalid LastDBPruneDate app variable Entry value found on startup.");
            _dBLastPrunedResult = new LastPruneDateResult(false, prunedEntry);
            return;
        }

        _dBLastPrunedResult = new LastPruneDateResult(validPruneDate, prunedEntry);
        _azDBManager.InitializeLastPruneDate(prunedEntry.Value);

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

        bool pruned = await TryDBPruneAsync(_defaultPruneRequest);

        if (pruned && _dBLastPrunedResult.Entry != null)
        {
            await UpdateLastPruneDateEntry();
            _dBLastPrunedResult.Entry.Value = DateTime.UtcNow.ToString("o");
            await _azTableManager.UpdateAppVarTableEntry(_dBLastPrunedResult.entry);
        }
        else if (pruned && _dBLastPrunedResult.entry == null)
        {
            var newLastPruneDateEntry = await _azTableManager.GetNewPruneDateEntry();
            await _azTableManager.AddAppVarTableEntry(newLastPruneDateEntry);
        }
    }

    // Validate the LastPruneDate App Var entry value (has a valid DateTime and is in the past)
    private static bool ValidatePruneDateEntry(AppVarEntry entry) =>
        DateTime.TryParse(entry.Value, out DateTime parsedDate) && parsedDate < DateTime.UtcNow;

    // Attempt to prune the database if needed based on the LastPruneDate App Var Result and AzDBManager's pruning conditions
    // If the Prune was completed successfully, returns true; otherwise, false.
    public async Task<bool> TryDBPruneAsync(PruneRequest pruneRequest)
    {
        bool missingLastPrune = _dBLastPrunedResult.Entry == null;
        bool invalidLastPrune = _dBLastPrunedResult.IsValid == false;
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

    private async Task<bool> UpdateLastPruneDateEntry()
    {
        if (_azDBManager.LastPruned == null)
        {
            _logger.LogWarning("Cannot update LastDBPruneDate entry: AzDBManager's LastPruned date is null.");
            return false;
        }



        //if (_dBLastPrunedResult.Entry == null)
        //{
        //    try
        //    {
        //        var newnewLastDBPruneDateEntry = _azTableManager.GetNewAppVarEntry();
        //        MakeLastPruneDateEntry(newnewLastDBPruneDateEntry, (DateTime)_azDBManager.LastPruned);
        //        await _azTableManager.AddAppVarTableEntry(newnewLastDBPruneDateEntry);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogWarning("An exception occurred when attempting to update LastDBPruneDate entry: {msg}", ex.Message);
        //        _logger.LogTrace("Error stack trace: {trace}", ex.StackTrace);
        //        return false;
        //    }
        //}
        //else
        //{
            try
            {
                await _azTableManager.UpdateAppVarTableEntry(updatedEntry);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("An exception occurred when attempting to add new LastDBPruneDate entry: {msg}", ex.Message);
                _logger.LogTrace("Error stack trace: {trace}", ex.StackTrace);
                return false;
            }
        }
    }

    private void MakeLastPruneDateEntry(AppVarEntry varEntry, DateTime dateTime)
    {
        varEntry.RowKey = "LastDBPruneDate";
        varEntry.TypeName = "DateTime";
        varEntry.Description = "The last date the database was pruned of old entries.";
        varEntry.Value = dateTime.ToString("o");
    }
}
