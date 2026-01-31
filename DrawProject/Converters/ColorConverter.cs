using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DrawProject.Converters;
public class ColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter,
        System.Globalization.CultureInfo culture)
    {
        string colorName = parameter as string;
        return colorName switch
        {
            "Red" => Colors.Red,
            "Blue" => Colors.Blue,
            "Green" => Colors.Green,
            "Black" => Colors.Black,
            _ => Colors.Black
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
