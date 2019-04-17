using System;
using System.Windows.Data;

namespace WallX.Converters
{
    public class CurrentDateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value != null && System.Convert.ToDateTime(value).ToString("yyyy-MM-dd") == DateTime.Now.ToString("yyyy-MM-dd");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
