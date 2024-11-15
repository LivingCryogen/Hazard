using System.Globalization;
using System.Windows.Data;

namespace View.Converters;

[ValueConversion(typeof(int), typeof(bool))]
public class TurnEqualsPlayerNumber : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length <= 0 ||
            values[0] is not int playerTurn ||
            values[1] is not int playerNumber)
            return false;
        if (playerTurn == playerNumber)
            return true;
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
