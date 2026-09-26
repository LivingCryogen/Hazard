using Microsoft.Extensions.Primitives;
using System.ComponentModel;
using System.Threading.Tasks;
using static HazardBackend.Queries.Browse.BrowseQuery;

namespace HazardBackend.Queries.Browse;

internal sealed class BrowseGameSessionsQuery : BrowseQuery
{
    internal enum FilterProperty
    {
        IsDemo,
        InstallId,
        PlayerName,
        StartTime,
        EndTime,
        Winner,
        GameId
    }
    internal enum SortProperty
    {
        IsDemo,
        InstallId,
        GameId,
        StartTime,
        EndTime,
        Winner,
        NumPlayers,
        NumActions,
        NumClaims,
        NumAttacks,
        NumMoves,
        NumTrades,
        NumAcquiredContinents
    }
    internal Sort<SortProperty>? SortDescriptor { get; }
    internal IReadOnlyList<Filter<FilterProperty>> Filters { get; } = [];

    private BrowseGameSessionsQuery(
        Sort<SortProperty>? sortDescriptor,
        IReadOnlyList<Filter<FilterProperty>> filters)
        : base(QueryEntityType.GameSession) 
    {
        SortDescriptor = sortDescriptor;
        Filters = filters;
    }

    internal static ParseResult<DbQuery> Parse(
        BaseDbQueryData baseData, 
        BrowseQueryData browseData,
        ParseResult<DbQuery> parseResult,
        ILogger logger)
    {
        SortProperty? sortProperty = null;
        SortDirection? sortDirection = browseData.SortDirection;

        // Parse Properties
        if (browseData.SortProperty != null && sortDirection != null)
        {
            if (!Enum.TryParse<SortProperty>(browseData.SortProperty, out SortProperty parsedSortProperty))
            {
                logger.LogWarning("Sort property {property} was invalid for Browse Query; defaulting to no sort.", browseData.SortProperty);
                parseResult.Warnings.Add($"Invalid sort property {browseData.SortProperty} for Browse Query; defaulting to no sort.");
            }
            else
            {
                sortProperty = parsedSortProperty;
                logger.LogInformation("Parsed sort property: {sortProperty}.", sortProperty);
            }
        }

        List<FilterProperty> filterProperties = [];
        foreach((string FilterProperty, FilterOperator Operator, string RawValue) filter in browseData.Filters)
        {
            if (!Enum.TryParse<FilterProperty>(filter.FilterProperty, out FilterProperty parsedFilterProperty))
            {
                logger.LogWarning("Invalid filter property: {property}. Skipping filter!", filter.FilterProperty);
                parseResult.Warnings.Add($"Invalid filter property: {filter.FilterProperty}. Skipping filter!");
                browseData.Filters.Remove(filter);
                continue;
            }
            else
            {
                filterProperties.Add(parsedFilterProperty);
                logger.LogInformation("Parsed filter property: {filterProperty}.", parsedFilterProperty);
            }
        }

        if (filterProperties.Count == 0 && browseData.Filters.Count > 0)
        {
            logger.LogWarning("No valid filter properties were parsed from the provided filters. Defaulting to unfiltered results!");
            parseResult.Warnings.Add("No valid filter properties were parsed from the provided filters. Defaulting to unfiltered results!");
            browseData.Filters.Clear();
        }

        if (browseData.Filters.Count <= 0)
        {
            logger.LogWarning("No filters were provided for the Browse Query. Ensure Pagination limits are applied!");
            parseResult.Warnings.Add("No filters were provided for the Browse Query. Ensure Pagination limits are applied!");
        }



        // Coerce Filter Property Values
        int currentErrorCount = parseResult.Errors.Count;
        List<object> coercedValues = CoerceFilterValues(filterProperties, browseData, parseResult, logger);
        currentErrorCount = parseResult.Errors.Count - currentErrorCount;
        if (currentErrorCount > 0)
        {
            return parseResult;
        }

        // Build Instance, return wrapped in ParseResult
        new BrowseGameSessionsQuery(
            
        };
    }

    private static List<object> CoerceFilterValues(List<FilterProperty> properties, BrowseQueryData browseData, ParseResult<DbQuery> parseResult, ILogger logger)
    {
        List<int> invalids = [];
        List<object> coercedValues = [];
        int numFilters = browseData.Filters.Count;


        if (properties.Count != numFilters)
        {
            logger.LogWarning("Count mismatch between parsed Filter Properties {nump} and available Filter Raw Values {numv}; defaulting to filter property count.",
                properties.Count, numFilters);
        }

        foreach ((string FilterProperty, FilterOperator Operator, string RawValue) filter in browseData.Filters)
        {
            int index = 0;
            string parsedPropName = properties[index].ToString();
            if (parsedPropName != filter.FilterProperty)
            {
                logger.LogError("Mismatch between parallel lists - parsed filter '{parsed}' and unparsed filter '{property}'. Aborting....", parsedPropName, filter.FilterProperty);
                parseResult.Errors.Add($"Mismatch between parallel lists - parsed filter '{parsedPropName}' and unparsed filter '{filter.FilterProperty}");
                return [];
            }


        }
    }
}
