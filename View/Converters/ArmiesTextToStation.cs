using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace View.Converters;

[ValueConversion(typeof(string), typeof(Rect))]
internal class ArmiesTextToStation : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string text)
            throw new ArgumentException("The object parameter to be converted was not a string.", nameof(value));
       
        int length = text.Length;
        
        if (parameter is Point position)
            return new Rect(position, new Size(length * 12, 21));
        
        return new Rect(new Point(0, 0), new Size(length * 12, 21));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
