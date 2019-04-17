using System;
using System.Linq;
using System.Windows.Data;

namespace WallX.Converters
{
    public class GetDatesFromMonthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DateTime dateTime = System.Convert.ToDateTime(value);
            return Enumerable.Range(1, DateTime.DaysInMonth(dateTime.Year, dateTime.Month)).Select(day => new DateTime(dateTime.Year, dateTime.Month, day)).ToList();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
