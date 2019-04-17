using NextGen.Controls;
using System;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace WallX.Converters
{
    public class LibraryItemIconsConverter : IValueConverter
    {
        private object GetVisibility(object value, int index)
        {
            if (!(value is string) || string.IsNullOrWhiteSpace(System.Convert.ToString(value)))
            {
                return Visibility.Collapsed;
            }

            string objValue = (string)value;
            string extension = Path.GetExtension(objValue);

            if ((NxgUtilities.IsValidVideoExtension(extension) && index == 0) || (NxgUtilities.IsValidPdfExtension(extension) && index == 1))
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return GetVisibility(value, System.Convert.ToInt32(parameter));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
