using AzProxy.Requests;
using AzProxy.Storage;
using AzProxy.Storage.AzureDB.Context;
using AzProxy.Storage.AzureDB.Entities;
using AzProxy.Storage.AzureTables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AzProxy.Storage.AzureDB.Services;

public class AzDBPruner
{
    private readonly ILogger<AzDBPruner> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _pruneAfterDuration;
    private readonly TimeSpan _pruneIncompleteGamesAfterDuration;
    private DateTime? _lastPruneDate;

    public bool? PruningDue => _lastPruneDate == null ? null : DateTime.UtcNow - _lastPruneDate >= _pruneAfterDuration;

    public AzDBPruner(ILogger<AzDBPruner> logger,
        IConfiguration config,
        IServiceProvider serviceProvider,
        StorageManager storageManager)
    {
        _logger = logger;
        if (!double.TryParse(config["PruneDBAfterDays"], out double pruneDays))
        {
            _logger.LogWarning("PruneDBAfterDays configuration invalid or missing; defaulting to 7 days.");
            pruneDays = 7;
        }
        else
            _pruneAfterDuration = TimeSpan.FromDays(pruneDays);

        if (!double.TryParse(config["PruneIncompleteGamesAfterDays"], out double incGamePruneDays))
        {
            _logger.LogWarning("PruneIncompleteGamesAfterDays configuration invalid or missing; defaulting to 90 days.");
            incGamePruneDays = 90;
        }
        else
            _pruneIncompleteGamesAfterDuration = TimeSpan.FromDays(incGamePruneDays);
    }

    // Prune old/incomplete game session entries from the database
    // If pruning changes are made and saved successfully, returns true; otherwise, false.
    public async Task<bool> PruneAsync(PruneRequest pruneRequest)
    {
        var now = DateTime.UtcNow;
        bool forced = pruneRequest.ForcePrune;
        bool pruneDemos = pruneRequest.PruneDemos;
        // Set pruning cutoff; if auto prune, use config value. If manual, use parameter if provided.
        DateTime cutoffDate = pruneRequest.DaysOffset.HasValue && forced 
            ? now.AddDays(-pruneRequest.DaysOffset.Value) 
            : now - _pruneAfterDuration;
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<GameStatsDbContext>();

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
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when pruning the database: {message}", ex.Message);
            return false;
        }
    }

    public void SetLastPruneDateFromString(string pruneDate)
    {
        _lastPruneDate = DateTime.TryParse(pruneDate, out DateTime parsedDate) ? parsedDate : null;
    }
}
