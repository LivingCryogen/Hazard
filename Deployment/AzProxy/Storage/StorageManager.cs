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
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AzProxy.Storage;

public class StorageManager : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHostApplicationLifetime _appLife;
    private readonly ILogger _logger;
    private readonly IBanCache _cache;
    private readonly AzTableManager _azTableManager;
    private readonly AzDBManager _azDBManager;
    private List<AppVarEntry> _appVars;
    private record LastPruneDateResult(bool isValid, AppVarEntry? entry);

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
        var lastPruneResult = FetchLastPruneDate();
        await TryDBPrune(lastPruneResult);
    }

    // Fetch the last prune date from app variables and a boolean indicating if it's valid
    private LastPruneDateResult FetchLastPruneDate()
    {
        try
        {
            var entry = _appVars.FirstOrDefault(e => e.RowKey == "LastDBPruneDate");

            // No previous prune date found
            if (entry is null)
            {
                _logger.LogWarning("No LastDBPruneDate app variable found.");
                return new LastPruneDateResult(isValid: false, entry: null);
            }

            // Previous prune date found; check if it's valid and return false if not.
            if (!DateTime.TryParse(entry.Value, out _))
            {
                _logger.LogWarning("Previous LastDBPruneDate app variable value invalid.");

                return new LastPruneDateResult(isValid: false, entry);
            }

            return new LastPruneDateResult(isValid: true, entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when checking if database prune is needed: {message}", ex.Message);
            throw;
        }
    }
    private async Task TryDBPrune(LastPruneDateResult pruneResult)
    {
        // Last prune date had a valid Entry but invalid DateTime value
        if (!pruneResult.isValid && pruneResult.entry != null)
        {
            if (await _azDBManager.Prune())
            {
                _logger.LogInformation("Database pruned successfully after invalid LastPruneDate value found; updating App Var entry.");
                pruneResult.entry.Value = DateTime.UtcNow.ToString("o");

                await _azTableManager.UpdateAppVarTableEntry(pruneResult.entry);
            }
            else
            {
                _logger.LogWarning("Database prune failed after invalid LastPruneDate value found.");
            }
        }
        else if (!pruneResult.isValid && pruneResult.entry == null) // No valid prune date Table Entry found
        {
            if (await _azDBManager.Prune())
            {
                _logger.LogInformation("Database pruned successfully after invalid LastPruneDate value found; updating App Var entry.");
                var newLastPruneDate = await _azTableManager.GetNewPruneDateEntry();

                await _azTableManager.AddAppVarTableEntry(newLastPruneDate);
            }
            else
            {
                _logger.LogWarning("Database prune failed after invalid LastPruneDate value found.");
            }
        }
        else if (pruneResult.isValid && pruneResult.entry == null) // Unexpected behavior - any valid Entry should have a valid DateTime value
        {
            _logger.LogWarning("FetchLastPruneDate returned true, but without an out param App Var; this is unexpected behavior. Attempting prune....");
            if (await _azDBManager.Prune())
            {
                _logger.LogInformation("Database pruned successfully after invalid LastPruneDate value found; updating App Var entry.");
                var newLastPruneDate = await _azTableManager.GetNewPruneDateEntry();

                await _azTableManager.AddAppVarTableEntry(newLastPruneDate);
            }
            else
            {
                _logger.LogWarning("Database prune failed after invalid LastPruneDate value found.");
            }
        }
        else if (pruneResult.isValid && pruneResult.entry != null)
        {
            // Both return value and out param are valid; check if prune is needed based on date
            if (!DateTime.TryParse(pruneResult.entry.Value, out DateTime lastPruned))
            {
                _logger.LogWarning("FetchLastPruneDate returned true, but the DateTime parse failed; this is unexpected behavior. Attempting prune....");
                if (await _azDBManager.Prune())
                {
                    _logger.LogInformation("Database pruned successfully after invalid LastPruneDate value found; updating App Var entry.");
                    pruneResult.entry.Value = DateTime.UtcNow.ToString("o");
                    await _azTableManager.UpdateAppVarTableEntry(pruneResult.entry);
                }
                else
                {
                    _logger.LogWarning("Database prune failed after invalid LastPruneDate value found.");
                }
                return;
            }
            if (_azDBManager.DBPruningDue(lastPruned))
            {
                
            }
        }
    }



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

    private async Task PruneAndUpdatePruneDate(LastPruneDateResult? pruneResult)
    {
        if (await _azDBManager.Prune())
        {
            _logger.LogInformation("Database pruned successfully; updating LastPruneDate App Var entry.");
            if (pruneResult)
            pruneResult.entry.Value = DateTime.UtcNow.ToString("o");
            await _azTableManager.UpdateAppVarTableEntry(pruneResult.entry);
        }
        else
        {
            _logger.LogWarning("Database prune failed.");
        }
    }
}
