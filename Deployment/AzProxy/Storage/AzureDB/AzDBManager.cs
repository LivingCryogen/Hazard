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

    public async Task<bool> Prune(bool includeDemos)
    {
        var pruneResult = await _dbPruner.Prune(includeDemos, false);
        if (pruneResult != null)
        {
            _logger.LogInformation("Database prune completed successfully at {pruneTime}.", pruneResult);
            return true;
        }
        else
        {
            _logger.LogWarning("Database prune did not complete successfully.");
            return false;
        }
    }
}
