using AzProxy.Requests;
using AzProxy.Storage.AzureDB.Context;
using AzProxy.Storage.AzureDB.Entities;
using AzProxy.Storage.AzureDB.Services;
using AzProxy.Storage.AzureTables;
using Microsoft.EntityFrameworkCore;

namespace AzProxy.Storage.AzureDB;

public class AzDBManager
{

    private readonly ILogger<AzDBManager> _logger;
    private readonly GameStatsDbContext _dbContext;
    private readonly AzDBPruner _dbPruner;

    public AzDBManager(IConfiguration config, ILogger<AzDBManager> logger, GameStatsDbContext dbContext, AzDBPruner dbPruner)
    {
        _logger = logger;
        _dbContext = dbContext;
        _dbPruner = dbPruner;
    }

    public DateTime? LastPruned => _dbPruner.LastPruneDate;
    public int? PruneAfterDays => _dbPruner.PruneAfterDays;

    // Determine if the database should be pruned of old entries
    public bool ShouldPrune()
    {
        if (_dbPruner.PruningDue == null)
        {
            _logger.LogInformation("No previous prune date found (defaults to pruning).");
            return true;
        }
        else
            return (bool)_dbPruner.PruningDue;
    }

    public async Task<bool> PruneAsync(PruneRequest pruneRequest)
    {
        if (await _dbPruner.PruneAsync(pruneRequest))
        {
            _logger.LogInformation("Database prune completed successfully at {pruneTime}.", DateTime.UtcNow);
            return true;
        }
        else
        {
            _logger.LogWarning("Database prune did not complete successfully.");
            return false;
        }
    }

    public bool InitializeLastPruneDate(string lastPruneDate) => _dbPruner.InitializeLastPruneDate(lastPruneDate);
}
