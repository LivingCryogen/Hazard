using HazardBackend.DbQueries.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HazardBackend.DbQueries;

public enum DbQueryType : int
{
    None = 0,
    Leaderboard = 1, // sorted and/or filtered PlayerStats by properties
    GameSession = 2, // specific game session, or game sessions sorted or filtered by some property (e.g. all games started after a certain date)
    PlayerStats = 3 // specific aggregate player stats, or player stats sorted or filtered by some property (e.g. all players with more than 100 games won)
}

public enum SortDirection
{
    Ascending,
    Descending
}

public enum QueryProperty
{
    None = 0,

    // Universal properties
    IsDemo,
    InstallId,
    PlayerName,

    // PlayerStats properties
    GamesStarted,
    GamesCompleted,
    GamesWon,
    FirstGameStarted,
    FirstGameCompleted,
    LastGameStarted,
    LastGameCompleted,
    TotalGamesDuration,
    AttacksWon,
    AttacksLost,
    AttacksTied,
    Conquests,
    Retreats,
    ForcedRetreats,
    AttackDiceRolled,
    DefenseDiceRolled,
    Moves,
    MaxAdvances,
    TradeIns,
    TotalOccupationBonus,

    // GameSession properties
    StartTime,
    EndTime,
    Winner,
    GameId
}


public class DbQuery
{
    public DbQueryType Type { get; init; }
    public QueryProperty FilterProperty { get; private set; }
    public object FilterValue { get; private set; }
    public QueryProperty SortProperty { get; private set; }
    public SortDirection SortDirection { get; private set; }
    public int MaxLength { get; private set; }

    private DbQuery(DbQueryType queryType,
        QueryProperty filterProperty,
        object filterValue,
        QueryProperty sortProperty,
        SortDirection sortDirection,
        int maxLength)
    {
        Type = queryType;
        FilterProperty = filterProperty;
        FilterValue = filterValue;
        SortProperty = sortProperty;
        SortDirection = sortDirection;
        MaxLength = maxLength;
    }

    public static ParseResult<DbQuery> TryCreate(
        string dbQueryType,
        string filterProperty,
        string filterValue,
        string sortProperty,
        string sortDirection,
        string maxLength,
        ILogger logger)
    {
        List<string> errors = [];

        if (!Enum.TryParse<DbQueryType>(dbQueryType, true, out var queryTypeResult) || queryTypeResult == DbQueryType.None)
            errors.Add($"Invalid DbQueryType: '{dbQueryType}'.");

        if (!Enum.TryParse<QueryProperty>(filterProperty, true, out var filterPropertyResult) || filterPropertyResult == QueryProperty.None)
            errors.Add($"Invalid FilterProperty: '{filterProperty}'.");

        if (!Enum.TryParse<QueryProperty>(sortProperty, true, out var sortPropertyResult) || sortPropertyResult == QueryProperty.None)
            errors.Add($"Invalid SortProperty: '{sortProperty}'.");

        if (!(Enum.TryParse<SortDirection>(sortDirection, true, out var sortDirectionResult)))
            errors.Add($"Invalid SortDirection: '{sortDirection}'. Must be 'Ascending' or 'Descending'.");

        if (!int.TryParse(maxLength, out var maxLengthResult) || maxLengthResult <= 0)
            errors.Add($"Invalid MaxLength: '{maxLength}'. Must be a positive integer.");

        if (errors.Count > 0)
        {
            logger.LogError("Failed to parse query parameters: {Errors}", string.Join("; ", errors));
            return new ParseResult<DbQuery>(false, null, [.. errors]);
        }

        var valueErrors = ValidateFilterValue(filterPropertyResult, filterValue, logger);

        DbQuery dbQuery = new(
            queryTypeResult,
            filterPropertyResult,
            sortPropertyResult,
            sortDirectionResult,
            maxLengthResult);

        var (Success, Errors) = DbQueryValidator.Validate(dbQuery, logger);

        if (!Success)
            return new ParseResult<DbQuery>(false, null, Errors);

        if (Errors.Length > 0)
        {
            logger.LogError("DbQuery Validation returned success despite errors: {Errors}", string.Join("; ", Errors));
            return new ParseResult<DbQuery>(false, null, Errors);
        }

        return new ParseResult<DbQuery>(true, dbQuery, Errors);
    }

    private static (bool Success, string? Error) ValidateFilterValue(QueryProperty filterProperty, string filterValue, ILogger logger)
    {
        List<string> errors = [];

        switch (filterProperty)
        {
            case QueryProperty.IsDemo:
                if (filterValue.Equals("true", StringComparison.OrdinalIgnoreCase) || filterValue.Equals("false", StringComparison.OrdinalIgnoreCase))
                    return (true, null);
                else
                    return (false, $"Filter value for {filterProperty} must be 'true' or 'false'.");
                
            case QueryProperty.InstallId:
                if (Guid.TryParse(filterValue, out _))
                    return (true, null);
                else
                    return (false, $"Filter value for {filterProperty} must be a valid GUID.");

            case QueryProperty.PlayerName:
                if (!string.IsNullOrWhiteSpace(filterValue))
                    return (true, null);
                else
                    return (false, $"Filter value for {filterProperty} cannot be empty.");

            case QueryProperty.TotalGamesDuration:
                    if (TimeSpan.TryParse(filterValue, out _))
                        return (true, null);
                    else
                        return (false, $"Filter value for {filterProperty} must be a valid TimeSpan.");

            case QueryProperty.StartTime:
                if (DateTime.TryParse(filterValue, out _))
                    return (true, null);
                else
                    return (false, $"Filter value for {filterProperty} must be a valid DateTime.");

            case QueryProperty.EndTime:
                if (DateTime.TryParse(filterValue, out _))
                    return (true, null);
                else
                    return (false, $"Filter value for {filterProperty} must be a valid DateTime.");

            default:
                logger.LogWarning("No specific validation implemented for filter property {FilterProperty}. Skipping filter value validation.", filterProperty);
                break;
        }
    }
}
