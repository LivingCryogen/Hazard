using Shared.Geography.Enums;
using System.Globalization;
using System.Windows.Data;

namespace View.Converters;

public class ContainsTerritoryConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        System.Diagnostics.Debug.WriteLine($"Converter called:");
        System.Diagnostics.Debug.WriteLine($"  value type: {value?.GetType()}, count: {(value as IEnumerable<TerrID>)?.Count()}");
        System.Diagnostics.Debug.WriteLine($"  parameter type: {parameter?.GetType()}, value: {parameter}");

        if (value is IEnumerable<TerrID> set && parameter is int territoryID)
        {
            bool result = set.Contains((TerrID)territoryID);
            System.Diagnostics.Debug.WriteLine($"  Territory {(TerrID)territoryID} selectable: {result}");
            return result;
        }
        System.Diagnostics.Debug.WriteLine($"  Returning false - type mismatch");
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
