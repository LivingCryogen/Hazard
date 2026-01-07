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
            var lastPruneDate = ShouldPruneDataBase(false);
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

    // Determine if the database should be pruned of old entries
    // If return is null, prune should be skipped. Otherwise, return object's "Value" property should be updated to current time once pruning is successful.
    public AppVarEntry? ShouldPruneDataBase(bool forcedPrune)
    {
        try
        {
            var lastDBPruneDateEntry = _appVars.FirstOrDefault(entry => entry.RowKey == "LastDBPruneDate");
            var now = DateTime.UtcNow;

            // No previous prune date found; need to prune and set the date
            if (lastDBPruneDateEntry == null)
            {
                _logger.LogWarning("No LastDBPruneDate app variable found.");

                return new AppVarEntry()
                {
                    PartitionKey = _appVarsPartitionKey,
                    RowKey = "LastDBPruneDate",
                    TypeName = "DateTime",
                    Description = "The last date the database was pruned of old entries.",
                    Timestamp = DateTime.UtcNow,
                    Value = now.ToString("o")
                };
            }

            // Previous prune date found; check if it's valid
            if (!DateTime.TryParse(lastDBPruneDateEntry.Value, out DateTime lastPruneDate))
            {
                _logger.LogWarning("Previous LastDBPruneDate app variable value invalid; pruning and setting new prune date.");

                lastDBPruneDateEntry.Value = DateTime.UtcNow.ToString("o");

                return lastDBPruneDateEntry;
            }

            if (forcedPrune == true)
            {
                _logger.LogInformation("Forced prune requested; pruning database.");
                return lastDBPruneDateEntry;
            }

            // Check if enough time has passed since the last prune
            if (DateTime.UtcNow - lastPruneDate >= _pruneAfterDuration)
                return lastDBPruneDateEntry;
            else
                return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when checking if database prune is needed: {message}", ex.Message);
            throw;
        }
    }

    // Prune old/incomplete game session entries from the database
    // If pruning is successful, returns the DateTime the prune was run. Otherwise, returns null.
    public async Task<DateTime?> PruneDataBase(bool pruneDemos, bool forcedPrune)
    {
        var now = DateTime.UtcNow;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GameStatsDbContext>();


            var cutoffDate = forcedPrune ? now : now - _pruneIncompleteGamesAfterDuration;
            _logger.LogInformation("Pruning incomplete games older than {duration}...", cutoffDate);

            List<GameSessionEntity>? staleGames;
            if (!pruneDemos)
            {
                staleGames = [.. dbContext.Set<GameSessionEntity>()
                    .Where(entry => !entry.EndTime.HasValue
                        && entry.StartTime < cutoffDate
                        && !entry.IsDemo)];
            }
            else
            {
                staleGames = [.. dbContext.Set<GameSessionEntity>()
                    .Where(entry => !entry.EndTime.HasValue
                        && entry.StartTime < cutoffDate)];
            }

            _logger.LogInformation("Pruning {count} incomplete games...", staleGames.Count);
            
            foreach (var game in staleGames)
            {
                _logger.LogInformation("Pruning incomplete game (Demo = {demo}) with ID {gameId} from install {installID}, started on {startTime}.",
                    game.IsDemo,
                    game.GameId,  
                    game.InstallId, 
                    game.StartTime);
            }

            dbContext.RemoveRange(staleGames);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when pruning the database: {message}", ex.Message);
            return null;
        }
        return now;
    }
}
