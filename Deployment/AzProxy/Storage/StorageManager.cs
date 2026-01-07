using AzProxy.Storage.AzureDB;
using AzProxy.Storage.AzureDB.Context;
using AzProxy.Storage.AzureDB.Entities;
using AzProxy.Storage.AzureTables;
using AzProxy.Storage.AzureTables.BanList;
using Azure;
using Azure.Data.Tables;
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

                await _azTableManager.PersistBan(address, ban);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Table Update failed: {message}", ex.Message);
        }

        _logger.LogInformation("Checking if the Az Database should be pruned....");
        try
        {
            bool validLastPruneDateTime = FetchLastPruneDate(out AppVarEntry? lastPruneEntry);
            if (lastPruneEntry == null)
            {
                if (await _azDBManager.Prune())
                {
                    // Create new DB prune date entry and add to App Vars
                }
                else
                {
                    _logger.LogWarning("No LastDBPruneDate entry found, but database prune failed.");
                }
            }
            else if (!validLastPruneDateTime)
            {

            }



            if (lastPruneDate != null)
            {
                _logger.LogInformation("Pruning incomplete Game database entries.... Next prune will occur after {duration}.", _pruneAfterDuration);
                var prunedTime = await PruneDataBase(false, false);
                if (prunedTime != null)
                    lastPruneDate.Value = ((DateTime)prunedTime).ToString("o");
                await UpdateAppVarTableEntry(lastPruneDate);
            }    
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database prune check or prune failed unexpectedly: {message}", ex.Message);
        }
    }

    // Fetch the last prune date from app variables
    // If return is true, App variables contained a valid last prune date, and will be set to out param;
    // If false, any out param will either be null (no previous prune date) or invalid (previous prune date invalid).
    public bool FetchLastPruneDate(out AppVarEntry? pruneDateEntry)
    {
        try
        {
            var lastDBPruneDateEntry = _appVars.FirstOrDefault(entry => entry.RowKey == "LastDBPruneDate");
            var now = DateTime.UtcNow;

            // No previous prune date found
            if (lastDBPruneDateEntry == null)
            {
                _logger.LogWarning("No LastDBPruneDate app variable found.");
                pruneDateEntry = null;
                return false;
            }

            // Previous prune date found; check if it's valid and return false if not.
            if (!DateTime.TryParse(lastDBPruneDateEntry.Value, out DateTime lastPruneDate))
            {
                _logger.LogWarning("Previous LastDBPruneDate app variable value invalid.");

                pruneDateEntry = lastDBPruneDateEntry;
                return false;
            }



            pruneDateEntry = lastDBPruneDateEntry;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when checking if database prune is needed: {message}", ex.Message);
            throw;
        }
    }
}
