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
    private readonly TimeSpan? _pruneAfterDuration  = null;
    private readonly TimeSpan _pruneIncompleteGamesAfterDuration;

    public DateTime? LastPruneDate { get; private set; }
    public bool Pruned { get; private set; } = false;
    public int? PruneAfterDays => _pruneAfterDuration?.Days;
    public bool? PruningDue => LastPruneDate == null ? null : DateTime.UtcNow - LastPruneDate >= _pruneAfterDuration;


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

        if (!ChooseCutoffDate(pruneRequest.DaysOffset, pruneRequest.ForcePrune, out DateTime cutoffDate))
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

            LastPruneDate = DateTime.UtcNow;
            Pruned = true;

            _logger.LogInformation("Pruning complete at {prunedate}", LastPruneDate);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when pruning the database: {message}", ex.Message);
            return false;
        }
    }

    public bool InitializeLastPruneDate(string pruneDate)
    {
        bool parsed = DateTime.TryParse(pruneDate, out DateTime parsedDate);
        if (!parsed)
        {
            _logger.LogWarning("Failed to parse last prune date from string: {pruneDate}", pruneDate);
            return false;
        }
        LastPruneDate = parsedDate;
        return true;
    }

    // Set pruning cutoff; if auto prune, use config value. If manual, use parameter if provided.
    // If both provided, manual parameter takes precedence. If neither value is available, reject the request.
    public bool ChooseCutoffDate(int? daysOffset, bool forced, out DateTime cutoffDate)
    {
        DateTime now = DateTime.UtcNow;
        bool hasConfig = _pruneAfterDuration != null;
        bool hasParam = daysOffset != null && daysOffset >= 0;

        if (!hasParam && !hasConfig)
        {
            if (forced)
            {
                _logger.LogInformation("Prune request is forced; using default cutoff date of NOW.");
                cutoffDate = now;
                return true;
            }

            _logger.LogWarning("No valid prune duration available from either configuration or request; prune rejected.");
            cutoffDate = default;
            return false;
        }

        TimeSpan pruneBefore;
        if (hasParam)
            pruneBefore = TimeSpan.FromDays((double)daysOffset!);
        else
            pruneBefore = (TimeSpan)_pruneAfterDuration!;

        cutoffDate = now - pruneBefore;
        return true;
    }
}
