using Microsoft.Extensions.Primitives;
using System.ComponentModel;
using System.Threading.Tasks;
using static HazardBackend.Queries.Browse.BrowseQuery;

namespace HazardBackend.Queries.Browse;

public sealed class BrowseGameSessionsQuery : BrowseQuery
{
    public enum FilterProperty
    {
        IsDemo,
        InstallId,
        PlayerName,
        StartTime,
        EndTime,
        Winner,
        GameId
    }
    public enum SortProperty
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
    public Sort<SortProperty>? SortDescriptor { get; init; }
    public List<Filter<FilterProperty>> Filters { get; init; } = [];

    public static ParseResult<DbQuery> Parse(
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
                parseResult.Errors.Add($"Invalid sort property {browseData.SortProperty} for Browse Query; defaulting to no sort.");
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
                parseResult.Errors.Add($"Invalid filter property: {filter.FilterProperty}. Skipping filter!");
                browseData.Filters.Remove(filter);
                continue;
            }
            else
            {
                filterProperties.Add(parsedFilterProperty);
                logger.LogInformation("Parsed filter property: {filterProperty}.", parsedFilterProperty);
            }
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
        new BrowseGameSessionsQuery()
        {
            SortDescriptor = (sortProperty == null || sortDirection == null) ?
                null :
                new Sort<SortProperty>() { Property = (SortProperty)sortProperty, Direction = (SortDirection)sortDirection }
        };
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
