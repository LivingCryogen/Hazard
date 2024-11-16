using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace View.Converters;

public class PlayerNumbertoColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        List<SolidColorBrush> colorsList = [.. ((ObservableCollection<SolidColorBrush>)values[0])];
        int playerOwner = (int)values[1];
        int numPlayers = colorsList.Count;

        if (numPlayers > 2) {
            if (playerOwner < 0)
                return Brushes.Transparent;
            else
                return new SolidColorBrush(colorsList[playerOwner].Color);
        }
        else if (numPlayers == 2) {
            if (playerOwner < -1)
                return Brushes.Transparent;
            else if (playerOwner == -1) {
                SolidColorBrush aiColor = new() { Color = Brushes.MediumPurple.Color };
                return aiColor;
            }
            else return new SolidColorBrush(colorsList[playerOwner].Color);
        }
        else return Brushes.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
