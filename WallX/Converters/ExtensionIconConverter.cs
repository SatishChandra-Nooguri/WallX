using System;
using System.IO;
using System.Windows.Data;
using WallX.Services;
using WallX.Helpers;

namespace WallX.Converters
{
    public class ExtensionIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is string) || string.IsNullOrWhiteSpace(System.Convert.ToString(value)))
            {
                return Constants.InternalExtensionIconsPath + "icon_jpg.png";
            }
            string valueExtension = Path.GetExtension(value.ToString()).Replace(".", "").Replace("xlsx", "xls").Replace("docx", "doc").Replace("pptx", "ppt");
            return Constants.InternalExtensionIconsPath + "icon_" + valueExtension + ".png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
