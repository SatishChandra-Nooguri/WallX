using System;
using System.Windows.Data;
using System.Windows.Media;

namespace WallX.Converters
{
    public class FontConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new BrushConverter().ConvertFromString((bool)value ? "#FF5787C5" : "#FFFFFFFF") as SolidColorBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}