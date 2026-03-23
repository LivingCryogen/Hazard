using HazardBackend.Requests;
using HazardBackend.Storage.AzureDB.Context;
using HazardBackend.Storage.AzureDB.Entities;
using HazardBackend.Storage.AzureDB.Services;
using HazardBackend.Storage.AzureDB.Services.Queries;
using HazardBackend.Storage.AzureTables;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HazardBackend.Storage.AzureDB;

public class AzDBManager
{

    private readonly ILogger<AzDBManager> _logger;
    private readonly IServiceProvider _serviceProvider;

    private readonly TimeSpan _pruneIncompleteGamesAfterDuration;
    private readonly TimeSpan? _pruneAfterDuration = null;

    internal DateTime? LastPruneDate { get; private set; }
    internal bool Pruned { get; private set; } = false;
    internal int? PruneAfterDays => _pruneAfterDuration?.Days;


    public AzDBManager(IConfiguration config, ILogger<AzDBManager> logger, IServiceProvider serviceProvider) 
    {
        _logger = logger; 

        if (!double.TryParse(config["PruneDBAfterDays"], out double pruneDays))
        {
            _logger.LogWarning("PruneDBAfterDays configuration invalid or missing; defaulting to 7 days.");
            pruneDays = 7;
        }

        _pruneAfterDuration = TimeSpan.FromDays(pruneDays);

        if (!double.TryParse(config["PruneIncompleteGamesAfterDays"], out double incGamePruneDays))
        {
            _logger.LogWarning("PruneIncompleteGamesAfterDays configuration invalid or missing; defaulting to 90 days.");
            incGamePruneDays = 90;
        }
        
        _pruneIncompleteGamesAfterDuration = TimeSpan.FromDays(incGamePruneDays);

        _serviceProvider = serviceProvider;
    }

    public void InitializeLastPruneDate(DateTime lastPruneDate)
    {
        LastPruneDate = lastPruneDate;
    }

    public async Task<QueryResult> HandleClientQuery(string query)
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<QueryHandler>();
        try
        {
            return await handler.HandleQueryAsync(query);
        }
        catch (Exception ex)
        {
            return new QueryResult($"An error occurred while processing the query: {ex.Message}.", null);
        }
    }

    // Determine if the database should be pruned of old entries
    public bool ShouldPrune()
    {
        if (LastPruneDate == null)
        {
            _logger.LogInformation("No previous prune date found (defaults to pruning).");
            return true;
        }
        
        return DateTime.UtcNow - LastPruneDate >= _pruneAfterDuration;
    }

    public async Task<bool> PruneAsync(PruneRequest pruneRequest)
    {
        using var scope = _serviceProvider.CreateScope();
        var pruner = scope.ServiceProvider.GetRequiredService<Pruner>();

        if (await pruner.PruneAsync(pruneRequest))
        {
            _logger.LogInformation("Database prune completed successfully at {pruneTime}.", DateTime.UtcNow);
            Pruned = true;
            LastPruneDate = DateTime.UtcNow;
            return true;
        }
        else
        {
            _logger.LogWarning("Database prune did not complete successfully.");
            return false;
        }
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
