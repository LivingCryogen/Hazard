using AzProxy.Storage.AzureDB.Context;
using AzProxy.Storage.AzureDB.Entities;
using AzProxy.Storage.AzureTables;

namespace AzProxy.Storage.AzureDB;

public class AzDBManager(ILogger<AzDBManager> logger)
{
    // Determine if the database should be pruned of old entries
    // If return is null, prune should be skipped. Otherwise, return object's "Value" property should be updated to current time once pruning is successful.
    public bool ShouldPrune()
    {

        //// Check if enough time has passed since the last prune
        //if (DateTime.UtcNow - lastPruneDate >= _pruneAfterDuration)
        //    return lastDBPruneDateEntry;
        //else
        //    return null;
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
