using System;
using System.Windows.Data;

namespace WallX.Converters
{
    public class TimespanToMinutesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !string.IsNullOrEmpty(System.Convert.ToString(value)) ? TimeSpan.Parse(value.ToString()).TotalMinutes.ToString("00") : "00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
