
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DrawProject.Converters
{
    public class ColorToColorNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorName)
            {
                return colorName switch
                {
                    "Red" => Colors.Red,
                    "Blue" => Colors.Blue,
                    "Green" => Colors.Green,
                    "Black" => Colors.Black,
                    "White" => Colors.White,
                    "Yellow" => Colors.Yellow,
                    "Cyan" => Colors.Cyan,
                    "Magenta" => Colors.Magenta,
                    _ => Colors.Red
                };
            }

            return Colors.Black;
        }
    }
}