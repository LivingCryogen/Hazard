using AzProxy.Storage.AzureDB.Context;
using AzProxy.Storage.AzureDB.Entities;
using AzProxy.Storage.AzureTables;

namespace AzProxy.Storage.AzureDB;

public class AzDBManager
{
    private readonly ILogger<AzDBManager> _logger;
    private readonly TimeSpan _pruneAfterDuration;
    private readonly TimeSpan _pruneIncompleteGamesAfterDuration;

    public AzDBManager(IConfiguration config, ILogger<AzDBManager> logger)
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

    /// Check if enough time has passed since the last prune to demand pruning the database
    public bool DBPruningDue(DateTime lastPruneDate)
    {
        if (DateTime.UtcNow - lastPruneDate >= _pruneAfterDuration)
            return true;
        else
            return false;
    }

    // Determine if the database should be pruned of old entries
    // If return is null, prune should be skipped. Otherwise, return object's "Value" property should be updated to current time once pruning is successful.
    public bool ShouldPrune()
    {


        try
        {
            var lastDBPruneDateEntry = _appVars.FirstOrDefault(entry => entry.RowKey == "LastDBPruneDate");
            var now = DateTime.UtcNow;

            // No previous prune date found; need to prune and set the date
            if (lastDBPruneDateEntry == null)
            {
                logger.LogWarning("No LastDBPruneDate app variable found.");

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
