using System;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace WallX.Converters
{
    public class StringReplaceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string returnObject = string.Empty;
            string param = System.Convert.ToString(parameter);

            if (value != null && (!string.IsNullOrWhiteSpace(param) || value is DateTime))
            {
                switch (param.ToLower())
                {
                    case "string":
                        if (!(value is DateTime))
                            returnObject = Regex.Replace(System.Convert.ToString(value), "[A-Za-z ]", "");
                        else
                            returnObject = System.Convert.ToDateTime(value).ToString("hh:mm");
                            break;
                    case "int":
                        if (!(value is DateTime))
                        returnObject = Regex.Replace(System.Convert.ToString(value), "[^A-Za-z ]", "");
                        else
                            returnObject = System.Convert.ToDateTime(value).ToString("tt");
                        break;
                    default:
                        returnObject = param;
                        break;
                }
            }
            return returnObject;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
