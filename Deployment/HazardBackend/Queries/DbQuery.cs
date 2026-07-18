using HazardBackend.Queries.Browse;
using HazardBackend.Queries.Lookup;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Data.Common;
using System.Reflection;
using System.Reflection.Metadata;

namespace HazardBackend.Queries;

public abstract class DbQuery
{
    protected class BaseDbQueryData(DbQueryType type, QueryEntityType EntityType)
    {
        public DbQueryType Type { get; set; } = type;
        public QueryEntityType EntityType { get; set; } = EntityType;
    }

    protected DbQuery(DbQueryType type, QueryEntityType entityType)
    {
        Type = type;
        EntityType = entityType;
    }

    public DbQueryType Type { get; init; }
    public QueryEntityType EntityType { get; init; }

    public static ParseResult<DbQuery> Parse(string queryString, ILogger logger)
    {
        ParseResult<DbQuery> parseResult = new(false, null, []);
        var queryDictionary = QueryHelpers.ParseQuery(queryString);

        if (queryDictionary == null || queryDictionary.Count == 0)
        {
            logger.LogError("Query parameters missing.");
            parseResult.Errors.Add("Query parameters missing.");
            return parseResult;
        }

        if (!queryDictionary.TryGetValue("querytype", out var typeString))
        {
            logger.LogError("Missing querytype parameter.");
            parseResult.Errors.Add("Missing querytype parameter.");
            return parseResult;
        }

        if (!Enum.TryParse<DbQueryType>(typeString, true, out var type))
        {
            logger.LogError($"Invalid querytype parameter. Expected: 'Browse' or 'Lookup'. Actual: {typeString}");
            parseResult.Errors.Add($"Invalid querytype parameter. Expected: 'Browse' or 'Lookup'. Actual: {typeString}");
            return parseResult;
        }

        if (!queryDictionary.TryGetValue("entity", out var entityTypeString))
        {
            logger.LogError("Missing entitytype parameter.");
            parseResult.Errors.Add("Missing entitytype parameter.");
            return parseResult;
        }

        if (!Enum.TryParse<QueryEntityType>(entityTypeString, true, out var entityType))
        {
            logger.LogError($"Invalid entitytype parameter. Expected: 'GameSession' or 'PlayerStats'. Actual: {entityTypeString}");
            parseResult.Errors.Add($"Invalid entitytype parameter. Expected: 'GameSession' or 'PlayerStats'. Actual: {entityTypeString}");
            return parseResult;
        }

        var baseParsedData = new BaseDbQueryData(type, entityType);
        logger.LogInformation("Base construction data for DbQuery created: QueryType {qtype}, EntityType {etype}", type, entityType);

        switch (type)
        {
            case (DbQueryType.Browse):
                return BrowseQuery.Parse(queryDictionary, baseParsedData, parseResult, logger);
            case (DbQueryType.Lookup):
                return LookupQuery.Parse(queryDictionary, baseParsedData, parseResult, logger);
            default:
                return new (false, null, ["Query parameters missing."]);
        }
    }
}
