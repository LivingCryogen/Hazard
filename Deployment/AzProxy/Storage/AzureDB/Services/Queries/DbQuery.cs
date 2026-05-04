using HazardBackend.Storage.AzureDB.Services.Queries.Validation;
using Microsoft.Extensions.Logging;

namespace HazardBackend.Storage.AzureDB.Services.Queries;

public enum DbQueryType : int
{
    None = 0,
    Leaderboard = 1,
    GameSession = 2,
    PlayerStats = 3,
    Action = 4
}

public class DbQuery
{
    public DbQueryType Type { get; init; }
    public string SortPropertyName { get; private set; }
    public string SortDirection { get; private set; } 
    public int ResponseLength { get; private set; } 
    
    private DbQuery(string queryTypeName, string propertyName, string sortDirection, int responseLength)
    {
        Type = Enum.Parse<DbQueryType>(queryTypeName);
        SortPropertyName = propertyName;
        SortDirection = sortDirection;
        ResponseLength = responseLength;
    }

    public static ParseResult<DbQuery> TryCreate(string[] queryParams, ILogger logger)
    {
        var (Success, Error) = DbQueryValidator.Validate(queryParams, logger);

        if (!Success)
            return new ParseResult<Query> (false, null, Error);

        Query query;
        try
        {
            query = new Query(queryParams[0], queryParams[1], queryParams[2], int.Parse(queryParams[2]));

            return new ParseResult<Query>(true, query, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Query object from query parameters.");
            return new ParseResult<Query>(false, null, "Query construction failed with error:" + ex.Message);
        }
    }
}
