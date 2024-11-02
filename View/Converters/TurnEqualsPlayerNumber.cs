using System.Globalization;
using System.Windows.Data;

namespace View.Converters;

[ValueConversion(typeof(int), typeof(bool))]
public class TurnEqualsPlayerNumber : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values != null && values.Length > 0) {
            if (values[0] is int playerTurn && values[1] is int playerNumber)
                if (playerTurn == playerNumber)
                    return true;
                else
                    return false;
            else return false;
        }
        else throw new ArgumentNullException(nameof(values));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
