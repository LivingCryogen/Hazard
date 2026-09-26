using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Primitives;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System.Reflection.Metadata;
using static HazardBackend.Queries.Browse.BrowseQuery;

namespace HazardBackend.Queries.Browse;

internal abstract class BrowseQuery(QueryEntityType entityType) : DbQuery(DbQueryType.Browse, entityType)
{
    internal class BrowseQueryData(
        SortDirection? sortDirection, 
        string? sortProperty,
        List<(string FilterProperty, FilterOperator Operator, string RawValue)> filters)
    {
        internal SortDirection? SortDirection { get; set; } = sortDirection;
        internal string? SortProperty { get; set; } = sortProperty;
        internal List<(string FilterProperty, FilterOperator Operator, string RawValue)> Filters { get; set; } = filters;
    }

    internal enum SortDirection
    {
        Ascending,
        Descending
    }

    internal enum FilterOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterThanOrEqual,
        LessThanOrEqual,
        Contains
    }

    internal sealed record Filter<T> (T Property, FilterOperator Operator, object Value) where T : struct, Enum;
    internal sealed record Sort<T> (T Property, SortDirection Direction) where T : struct, Enum;

    internal static ParseResult<DbQuery> Parse(IQueryCollection queryCollection, BaseDbQueryData baseData, ParseResult<DbQuery> parseResult, ILogger logger)
    {
        SortDirection? sortDirection = null;

        // Sort Direction
        if (!queryCollection.TryGetValue("sortdirection", out StringValues directionParams))
        {
            logger.LogWarning("Missing sort direction parameter for Browse Query; defaulting to no sort.");
            parseResult.Errors.Add("Missing sort direction parameter for Browse Query; defaulting to no sort.");
        }
        else
        {
            if (directionParams.Count > 1)
            {
                directionParams = directionParams.FirstOrDefault();
                logger.LogWarning("Multiple sort direction parameters detected; defaulting to the first: {param}", directionParams[0]);
                parseResult.Errors.Add($"Multiple sort direction parameters detected; defaulting to the first: {directionParams[0]}");
            }

            if (!Enum.TryParse<SortDirection>(directionParams[0], true, out SortDirection enumDirection))
            {
                logger.LogWarning("Invalid sort direction parameter; defaulting to Descending.");
                parseResult.Errors.Add("Invalid sort direction parameter; defaulting to Descending.");
                sortDirection = SortDirection.Descending;
            }
            else
            {
                sortDirection = enumDirection;
                logger.LogInformation("Sort direction parsed as '{direction}'.", sortDirection);
            }
        }

        // Filters (including basic prop data)
        if (!queryCollection.TryGetValue("filter", out StringValues filterProperties) ||  filterProperties.IsNullOrEmpty())
        {
            logger.LogError("No filter properties parameters found for Browse Query!");
            parseResult.Errors.Add("No filter properties parameters found for Browse Query!");
            return parseResult;
        }

        if (!queryCollection.TryGetValue("op", out StringValues filterOperators) || filterOperators.IsNullOrEmpty())
        {
            logger.LogError($"No filter operator parameters found for filters {filterProperties}!");
            parseResult.Errors.Add($"No filter operator parameters found for filters {filterProperties}!");
            return parseResult;
        }

        if (!queryCollection.TryGetValue("val", out StringValues filterValues) || filterValues.IsNullOrEmpty())
        {
            logger.LogError("No filter values found for filters!");
            parseResult.Errors.Add($"No filter values found for filters {filterProperties}!");
            return parseResult;
        }

        var filters = NormalizeFilterParams(filterProperties, filterOperators, filterValues, parseResult, logger);
        logger.LogInformation("{num} filters normalized.", filters.Length);
        var filtersWithParsedOps = ParseOperators(filters, parseResult);
        logger.LogInformation("{num} filters normalized with parsed operators.", filtersWithParsedOps.Count);

        // Basic Direction Property Data (not yet Enum parsing - that's for the subclass)
        if (!queryCollection.TryGetValue("sort", out StringValues sortParams) || sortParams.IsNullOrEmpty())
        {
            logger.LogWarning("Missing sort property for Browse Query; defaulting to no sort.");
            parseResult.Errors.Add("Missing sort property for Browse Query; defaulting to no sort.");
            sortDirection = null;
        }
        else if (sortParams.Count > 1)
        {
            logger.LogWarning("Excess sort properties for Browse Query; defaulting to the first provided: {sortParam}.", sortParams[0]);
            parseResult.Errors.Add($"Excess sort properties for Browse Query; defaulting to the first provided: {sortParams[0]}.");
            if ((sortParams[0].IsNullOrEmpty()))
            {
                logger.LogWarning("Sort property was null or empty for Browse Query; defaulting to no sort.");
                parseResult.Errors.Add("Sort property was null or empty for Browse Query; defaulting to no sort.");
                sortDirection = null;
            }
        }
        else if (sortParams.Count == 1)
        {
            if ((sortParams[0].IsNullOrEmpty()))
            {
                logger.LogWarning("Sort property was null or empty for Browse Query; defaulting to no sort.");
                parseResult.Errors.Add("Sort property was null or empty for Browse Query; defaulting to no sort.");
                sortDirection = null;
            }
        }

        BrowseQueryData browseQueryData = new(sortDirection, sortParams[0] ?? null, filtersWithParsedOps);
        logger.LogInformation("Browse Query data created: SortDirection {direction}; SortProperty {sort}; {numFilters}.", sortDirection, sortParams[0], filtersWithParsedOps.Count);

        switch (baseData.EntityType)
        {
            case (QueryEntityType.GameSession):
                return BrowseGameSessionsQuery.Parse(baseData, browseQueryData, parseResult, logger);
            case (QueryEntityType.PlayerStats):
                return BrowsePlayerStatsQuery.Parse(baseData, browseQueryData, parseResult, logger);
        }
    }

    private static (string FilterProperty, string Operator, string RawValue)[] NormalizeFilterParams(
        StringValues filterProperties,
        StringValues filterOperators,
        StringValues filterValues, 
        ParseResult<DbQuery> parseResult,
        ILogger logger)
    {

        // Ensure All Filters Have Requisite Operators and Values
        int numFilterProperties = filterProperties.Count;
        int numFilterOperators = filterOperators.Count;
        int numFilterValues = filterValues.Count;

        // Check against any null or empty values (since StringValues allows)
        List<string> filterPropList = [];
        for (int i = 0; i < numFilterProperties; i++)
        {
            var property = filterProperties[i];
            if (property == null || property == "")
            {
                logger.LogWarning("Filter property name parameter #{i} was null or empty; Skipping incomplete filters.", i);
                parseResult.Errors.Add($"Filter oroperty name parameter #{i} was null or empty; Skipping incomplete filters.");
                continue;
            }

            filterPropList.Add(property); 
        }
        numFilterProperties = filterPropList.Count;

        List<string> filterOpList = [];
        for (int i = 0; i < numFilterOperators; i++)
        {
            var op = filterOperators[i];
            if (op == null || op == "")
            {
                logger.LogWarning("Filter operator parameter #{i} was null or empty; Skipping incomplete filters.", i);
                parseResult.Errors.Add($"Filter operator parameter #{i} was null or empty; Skipping incomplete filters.");
                continue;
            }

            filterOpList.Add(op);
        }
        numFilterOperators = filterOpList.Count;

        List<string> filterValuesList = [];
        for (int i = 0; i < numFilterValues; i++)
        {
            var value = filterValues[i];
            if (value == null || value == "")
            {
                logger.LogWarning("Filter value parameter #{i} was null or empty; Skipping incomplete filters.", i);
                parseResult.Errors.Add($"Filter value parameter #{i} was null or empty; Skipping incomplete filters.");
                continue;
            }

            filterValuesList.Add(value);
        }
        numFilterValues = filterValuesList.Count;


        // Ensure equal length of parallel lists, always trimming (never adding)
        string propListString = string.Join(", ", filterPropList);
        if (numFilterProperties > numFilterOperators)
        {
            logger.LogWarning("Not enough filter operators for filters {propList}. Skipping incomplete filters.", propListString);
            parseResult.Errors.Add($"Not enough filter operators for filters {propListString}. Skipping incomplete filters.");
            filterPropList = [.. filterPropList.Take(numFilterOperators)];
            numFilterProperties = filterPropList.Count;
        }

        if (numFilterProperties > numFilterValues)
        {
            parseResult.Errors.Add($"Not enough filter values for filters {propListString}. Skipping incomplete filters.");
            filterPropList = [.. filterPropList.Take(numFilterValues)];
            numFilterProperties = filterPropList.Count;
        }

        if (numFilterProperties < numFilterOperators)
        {
            parseResult.Errors.Add($"Excess filter operators for filters {propListString}. Skipping incomplete filters.");
            filterOpList = [.. filterOpList.Take(numFilterProperties)];
            numFilterOperators = filterOpList.Count;
        }

        if (numFilterProperties < numFilterValues)
        {
            parseResult.Errors.Add($"Excess filter values for filters {propListString}. Skipping incomplete filters.");
            filterValuesList = [.. filterValuesList.Take(numFilterProperties)];
            numFilterValues = filterValuesList.Count;
        }

 
        var filters = new (string Property, string Operator, string RawValue)[numFilterProperties];
        for (int i = 0; i < numFilterProperties; i++)
        {
            filters[i] = (filterPropList[i], filterOpList[i], filterValuesList[i]);
        }

        return filters;
    }

    private static List<(string Property, FilterOperator Operator, string RawValue)> ParseOperators(
        (string Property, string Operator, string Value)[] filters,
        ParseResult<DbQuery> parseResult)
    {
        List<(string Property, FilterOperator Operator, string RawValue)> parsedOpFilters = [];
        for (int i = 0; i < filters.Length; i++)
        {
            if (!Enum.TryParse<FilterOperator>(filters[i].Operator, out var parsedOperator))
            {
                parseResult.Errors.Add($"Filter of property {filters[i].Property} had an invalid operator: {filters[i].Operator}; Skipping invalid filters.");
                continue;
            }
            else
                parsedOpFilters.Add((filters[i].Property, parsedOperator, filters[i].Value));
        }

        return [.. parsedOpFilters];
    }
}