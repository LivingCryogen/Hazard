using HazardBackend.DbQueries.Validation;
using Microsoft.Extensions.Logging;

namespace HazardBackend.DbQueries;

public enum DbQueryType : int
{
    None = 0,
    Leaderboard = 1, // sorted PlayerStats by some property
    GameSession = 2, // specific game session, or game sessions filtered by some property (e.g. all games started after a certain date)
    PlayerStats = 3 // specific aggregate player stats, or player stats filtered by some property (e.g. all players with more than 100 games won)
}

public enum SortDirection
{
    Ascending,
    Descending
}

public class DbQuery
{
    public DbQueryType Type { get; init; }
    public string SortPropertyName { get; private set; }
    public SortDirection SortDirection { get; private set; } = SortDirection.Descending; // Default to descending
    public int ResponseLength { get; private set; } 
    
    private DbQuery(string queryTypeName, string propertyName, string sortDirection, int responseLength)
    {
        Type = Enum.Parse<DbQueryType>(queryTypeName);
        SortPropertyName = propertyName;
        SortDirection = sortDirection;
        ResponseLength = responseLength;
    }

    public static ParseResult<DbQuery> TryCreate(DbQueryType type, string? sortBy, bool? descending, int? maxLength, ILogger logger)
    {
        var (Success, Error) = DbQueryValidator.Validate(queryParams, logger);

        if (!Success)
            return new ParseResult<DbQuery> (false, null, Error);

        DbQuery query;
        try
        {
            query = new DbQuery(queryParams[0], queryParams[1], queryParams[2], int.Parse(queryParams[2]));

            return new ParseResult<DbQuery>(true, query, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create Query object from query parameters.");
            return new ParseResult<DbQuery>(false, null, "Query construction failed with error:" + ex.Message);
        }
    }
}
