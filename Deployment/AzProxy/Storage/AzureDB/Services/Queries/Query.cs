using HazardBackend.Storage.AzureDB.Services.Queries.Validation;
using Microsoft.Extensions.Logging;

namespace HazardBackend.Storage.AzureDB.Services.Queries;

public enum QueryType : int
{
    None = 0,
    Leaderboard = 1,
    GameSession = 2,
    PlayerStats = 3,
    Action = 4
}

public class Query
{
    private readonly ILogger<Query> _logger;
    private string _sortDirection = "ascending"; // Default sort direction if not specified in query parameters.
    private int _responseLength = 10; // Default response length if not specified in query parameters.
    
    public QueryType Type { get; init; }
    public string SortDirection { get => _sortDirection; }
    public int ResponseLength { get => _responseLength; }

    private Query(string[] queryParams, HashSet<string>? validValuesCache, ILogger<Query> logger)
    {
        _logger = logger;

        if (!ValidateParams(queryParams, validValuesCache, out string? error))
        {
            Type = QueryType.None;
            Result = new QueryResult(error, null);
        }
    }

    public static ParseResult<Query> TryCreate(string[] queryParams, HashSet<string>? validValuesCache)
    {

    }

    private bool ValidateParams(, out string? error)
    {
        if (queryParams == null)
        {
            error = "Query parameters cannot be null.";
            return false;
        }

        int paramsLength = queryParams.Length;
        if (paramsLength == 0)
        {
            error = "Empty leaderboard query parameter; no data fetched.";
            return false;
        }

        if (string.IsNullOrEmpty(queryParams[0]))
        {
            error = "Empty leaderboard sort parameter; no data fetched.";
            return false;
        }

        if (validValuesCache != null && !validValuesCache.Contains(queryParams[0]))
        {
            error = $"Invalid leaderboard sort parameter; '{queryParams[0]}' not found in cache; no data fetched.";
            return false;
        }

        bool validSortParam = true;
        if (paramsLength > 1 &&
            string.IsNullOrEmpty(queryParams[1]))
        {
            _logger.LogWarning("Empty sort direction parameter; defaulting to 'ascending.'");
            validSortParam = false;
        }

        if (paramsLength > 1 &&
            queryParams[1] != "ascending"
            && queryParams[1] != "descending")
        {
            _logger.LogWarning("Invalid sort direction parameter '{directionParam}'; defaulting to 'ascending.'", queryParams[1]);
            validSortParam = false;
        }

        if (validSortParam)
            _sortDirection = queryParams[1];

        if (paramsLength > 2)
        {
            if (string.IsNullOrEmpty(queryParams[2]))
                _logger.LogWarning("Empty response length parameter; defaulting to 10.");
            else if (!int.TryParse(queryParams[2], out int responseLength))
                _logger.LogWarning("Invalid response length parameter '{queryParams[2]}'; defaulting to 10.", queryParams[2]);
            else
                _responseLength = responseLength;
        }

        if (paramsLength > 3)
            _logger.LogWarning($"Too many parameters for leaderboard query; only the first three will be used.");

        error = null;
        return true;
    }
}
