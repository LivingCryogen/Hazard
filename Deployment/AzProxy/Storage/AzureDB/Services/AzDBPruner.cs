using AzProxy.Storage;
using AzProxy.Storage.AzureTables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AzProxy.Storage.AzureDB.Services;

public class AzDBPruner
{
    private readonly ILogger<AzDBPruner> _logger;

    public AzDBPruner(ILogger<AzDBPruner> logger, IConfiguration config, StorageManager storageManager)
    {
        _logger = logger;

    }


    [Authorize(Policy = "AdminOnly")]
    public static async Task<IResult> PruneAsync(IQueryCollection requestQueries,
        StorageManager storageManager)
    {
        // Get pruneDemos flag from query
        bool pruneDemos = false;
        if (requestQueries.TryGetValue("pruneDemos", out var demoValue))
        {
            if (bool.TryParse(demoValue, out bool parsedDemosFlag))
                pruneDemos = parsedDemosFlag;
        }

        // Get forcePrune flag from query
        bool forcedPrune = false;
        if (requestQueries.TryGetValue("force", out var force))
        {
            if (bool.TryParse(force, out bool forced))
                forcedPrune = forced;
        }

        try
        {
            AppVarEntry appVarLastPrune = storageManager.ShouldPruneDataBase(forcedPrune)
                ?? throw new InvalidOperationException("ShouldPrune method returned null even during manual prune flow!");

            if (appVarLastPrune == null)
            {
                logger.LogInformation("Prune skipped; Demos : {demoflag}. Forced : {forcedPrune}.", pruneDemos, forcedPrune);
                return Results.Accepted("Prune skipped.");
            }

            var prunedTime = await storageManager.PruneDataBase(pruneDemos, forcedPrune);

            if (prunedTime != null)
            {
                appVarLastPrune.Value = ((DateTime)prunedTime).ToString("o");
                await storageManager.UpdateAppVarTableEntry(appVarLastPrune);

                logger.LogInformation("Prune successful; Demos : {demoflag}. Forced : {forcedPrune}.", pruneDemos, forcedPrune);
                return Results.Ok("Prune completed.");
            }
            else
            {
                logger.LogInformation("Prune skipped or failed; Demos : {demoflag}. Forced : {forcedPrune}.", pruneDemos, forcedPrune);
                return Results.Problem("Prune skipped or failed.", statusCode: StatusCodes.Status202Accepted);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during the prune operation: {Message}", ex.Message);
            return Results.Problem("An error occurred during the prune operation.", statusCode: StatusCodes.Status500InternalServerError);

        }
    }
}
