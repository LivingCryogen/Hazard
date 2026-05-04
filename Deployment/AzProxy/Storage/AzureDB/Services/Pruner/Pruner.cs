using HazardBackend.Storage.AzureDB.Context;
using HazardBackend.Storage.AzureDB.Entities;
using HazardBackend.Storage.AzureTables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace HazardBackend.Storage.AzureDB.Services.Pruner;

public class Pruner(ILogger<Pruner> logger,
    IServiceProvider serviceProvider,
    AzDBManager dBManager)
{
    private readonly ILogger<Pruner> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly AzDBManager _dbManager = dBManager;

    // Prune old/incomplete game session entries from the database
    // If pruning changes are made and saved successfully, returns true; otherwise, false.
    public async Task<bool> PruneAsync(PruneRequest pruneRequest)
    {
        var now = DateTime.UtcNow;
        bool forced = pruneRequest.ForcePrune;
        bool pruneDemos = pruneRequest.PruneDemos;

        if (!_dbManager.ChooseCutoffDate(pruneRequest.DaysOffset, pruneRequest.ForcePrune, out DateTime cutoffDate))
        {
            _logger.LogWarning("Prune request rejected: no valid cutoff date could be determined.");
            return false;
        }

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
}
