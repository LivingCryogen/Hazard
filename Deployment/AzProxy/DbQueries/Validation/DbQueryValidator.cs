using HazardBackend.DbQueries;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HazardBackend.DbQueries.Validation;

public static class DbQueryValidator
{
    /* Validates the query parameters for a query. Query params should have the form:
     *      [query type, sorting property name, sort direction, response length]
     *     Example:
     *      ["leaderboard", "GamesWon", "descending", "25"]
     *      
     *      Returns a flag indicating whether validation was successful or not, with possible error message.
     *      
     *      NOTE!! : Sort direction or response length set to default ("ascending" and/or "10") if invalid or missing!  */
    public static (bool Success, string? error) Validate(List<string> queryParams, HashSet<string> propertyNames, ILogger logger)
    {
        if (queryParams == null)
            return (false, "Query parameters cannot be null.");
        int paramsLength = queryParams.Count;
        if (paramsLength == 0)
            return (false, "Empty query parameter; no data fetched.");
        if (string.IsNullOrEmpty(queryParams[0]))
            return (false, "Empty query type parameter; no data fetched.");
        if (Enum.TryParse(typeof(DbQueryType), queryParams[0], ignoreCase: true, out _))
            return (false, $"Invalid query:'{queryParams[0]}' is not a valid query type; no data fetched.");

        string queryTypeName = queryParams[0];

        if (paramsLength < 2 || string.IsNullOrEmpty(queryParams[1]))
            return (false, "Empty query property name; no data fetched.");
        if (!propertyNames.Contains(queryParams[1]))
            return (false, $"Invalid query property name; '{queryParams[1]}' is not a valid property for type {queryParams[0]}; no data fetched.");)

        string sortPropertyName = queryParams[1];

        string sortDirection = "ascending";
        int responseLength = 10;
        if (paramsLength < 3)
        { 
            logger.LogWarning("Empty sort direction and response length parameters; defaulting to 'ascending' and '10'.");
            queryParams.Add("ascending");
            queryParams.Add("10");
            return (true, null);
        }

        if (paramsLength > 3 && string.IsNullOrEmpty(queryParams[1]))
            logger.LogWarning("Empty sort direction parameter; defaulting to 'ascending.'");
        

        if (paramsLength > 3 &&
            queryParams[2] != "ascending"
            && queryParams[2] != "descending")
        {
            logger.LogWarning("Invalid sort direction parameter '{directionParam}'; defaulting to 'ascending.'", queryParams[1]);
            queryParams[1] = "ascending";
        }

        if (paramsLength > 2)
        {
            if (string.IsNullOrEmpty(queryParams[2]))
            {
                logger.LogWarning("Empty response length parameter; defaulting to 10.");
                queryParams[2] = "10";
            }
            else if (!int.TryParse(queryParams[2], out int _))
            {
                logger.LogWarning("Invalid response length parameter '{queryParams[2]}'; defaulting to 10.", queryParams[2]);
                queryParams[2] = "10";
            }
        }

        if (paramsLength > 3)
        {
            logger.LogWarning($"Too many parameters for leaderboard query; only the first three will be used.");
        }

        return (true, null);
    }
}
