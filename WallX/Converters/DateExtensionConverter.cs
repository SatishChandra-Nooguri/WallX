using NextGen.Controls;
using System;
using System.Windows.Data;

namespace WallX.Converters
{
    public class DateExtensionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (string.IsNullOrWhiteSpace(System.Convert.ToString(parameter)))
                return NxgUtilities.GetDateExtension(System.Convert.ToDateTime(value).ToString("dd"));
            else if (System.Convert.ToString(parameter) == "Day")
                return System.Convert.ToDateTime(value).ToString("dddd ") + NxgUtilities.GetDateExtension(System.Convert.ToDateTime(value).ToString("dd")) + System.Convert.ToDateTime(value).ToString(" MMMM yyyy");
            else
                return NxgUtilities.GetDateExtension(System.Convert.ToDateTime(value).ToString("dd")) + System.Convert.ToDateTime(value).ToString(" MMMM yyyy");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
