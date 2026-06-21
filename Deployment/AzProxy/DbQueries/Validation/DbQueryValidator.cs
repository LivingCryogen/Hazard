using HazardBackend.DbQueries;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HazardBackend.DbQueries.Validation;

public static class DbQueryValidator
{
    /* Validates the query parameters for a query.
     * Parsing is handled by the DbQuery.TryCreate method, so this method only needs to check for logical consistency of the parameters 
     * (e.g. that the filter property is valid for the query type, that the sort direction is valid for the sort property, etc.).
     */
    public static (bool Success, string[] Errors) Validate(DbQuery query, ILogger logger)
    {
        return query.Type switch 
        {
            DbQueryType.Leaderboard => ValidateLeaderboardQuery(query, logger),
            DbQueryType.PlayerStats => ValidatePlayerStatsQuery(query, logger),
            // ...
        }

    }

    private static (bool Success, string[] Errors) ValidateLeaderboardQuery(DbQuery query, ILogger logger)
    {
        List<string> errors = [];
        
        // For a leaderboard query, the filter property must be "None", "IsDemo", or "InstallId" (for local machine leaderboard)
        if (query.FilterProperty != QueryProperty.None
            && query.FilterProperty != QueryProperty.IsDemo
            && query.FilterProperty != QueryProperty.InstallId)
            errors.Add($"Invalid FilterProperty for Leaderboard query: '{query.FilterProperty}'. If not 'None', it must be 'IsDemo' or 'InstallId'.");

        // For a leaderboard query, the sort property must NOT be "None"
        if (query.SortProperty == QueryProperty.None)
            errors.Add($"Invalid SortProperty for Leaderboard query: '{query.SortProperty}'. Must not be 'None'.");

        // For a leaderboard query, the sort property must NOT be one of the GameSession properties (StartTime, EndTime, Winner, GameId).
        if (query.SortProperty == QueryProperty.StartTime
            || query.SortProperty == QueryProperty.EndTime
            || query.SortProperty == QueryProperty.Winner
            || query.SortProperty == QueryProperty.GameId)
            errors.Add($"Invalid SortProperty for Leaderboard query: '{query.SortProperty}'. Should not be a GameSession property (StartTime, EndTime, Winner, GameId).");

        if (errors.Count > 0)
        {
            logger.LogWarning("Validation failed for query {Query}. Errors: {Errors}", query, errors);
            return (false, [.. errors]);
        }

        return (true, [.. errors]);
    }

    private static (bool Success, string[] Errors) ValidatePlayerStatsQuery(DbQuery query, ILogger logger)
    {
        List<string> errors = [];
        
        // For a player stats query, the filter property must be "IsDemo," "PlayerName", or "InstallId"
        if (errors.Count > 0)
        {
            logger.LogWarning("Validation failed for query {Query}. Errors: {Errors}", query, errors);
            return (false, [.. errors]);
        }
        return (true, [.. errors]);
    }
}
