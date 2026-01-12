using AzProxy.Storage.AzureDB.Context;
using AzProxy.Storage.AzureDB.Entities;
using AzProxy.Storage.AzureTables;
using Microsoft.EntityFrameworkCore;

namespace AzProxy.Storage.AzureDB;

public class AzDBManager
{

    private readonly ILogger<AzDBManager> _logger;
    private readonly GameStatsDbContext _dbContext;
    private readonly TimeSpan _pruneAfterDuration;
    private readonly TimeSpan _pruneIncompleteGamesAfterDuration;
    private DateTime? _lastPruneDate;

    public AzDBManager(IConfiguration config, ILogger<AzDBManager> logger, GameStatsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;

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

    public void SetLastPruneDateFromString(string pruneDate)
    {
        _lastPruneDate = DateTime.TryParse(pruneDate, out DateTime parsedDate) ? parsedDate : null;
    }

    // Determine if the database should be pruned of old entries
    public bool ShouldPrune()
    {
        if (_lastPruneDate == null)
        {
            _logger.LogInformation("No previous prune date found for azDBManager's pruning conditional (defaults to false).");
            return false;
        }

        return DBPruningDue();
    }

    /// Check if enough time has passed since the last prune to demand pruning the database
    public bool DBPruningDue() => DateTime.UtcNow - _lastPruneDate >= _pruneAfterDuration;

    // Prune old/incomplete game session entries from the database
    // If pruning is successful, returns the DateTime the prune was run. Otherwise, returns null.
    public async Task<DateTime?> Prune(bool pruneDemos, bool forcedPrune)
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
