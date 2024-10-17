using System.Globalization;
using System.Windows.Data;

namespace View.Converters;
public class PlayerTurnToCardFace : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        int playerTurn = (int)values[0];
        int ownerNumber = (int)values[1];
        Face cardFace = (Face)values[2];

        if (playerTurn.Equals(ownerNumber))
            return cardFace;
        else return Face.Null;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
