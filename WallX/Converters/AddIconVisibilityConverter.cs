using System;
using System.Windows.Data;
using WallX.Services;
using WallX.Helpers;

namespace WallX.Converters
{
    public class AddIconVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int availableMinutes = 0;
            if (value != null && value is Class)
            {
                Class classInfo = (Class)value;

                if (classInfo.StartTime.Date >= DateTime.Now.Date)
                {
                    if (parameter.ToString() == "icon_PLUS_Carosel_right" && classInfo.StartTime > DateTime.Now &&
                        classInfo.PreviousClassEndTime.ToString("hh:mm tt") != Constants.DayStartTime && classInfo.PreviousClassEndTime != classInfo.StartTime &&
                        (classInfo.StartTime > DateTime.Now || DateTime.Now.Subtract(classInfo.EndTime).TotalMinutes < 0) &&
                        classInfo.StartTime > classInfo.PreviousClassEndTime)
                    {
                        availableMinutes = TimeGapBetweenMeetings(classInfo.StartTime, classInfo.PreviousClassEndTime);
                    }
                    else if (parameter.ToString() == "icon_PLUS_Carosel_left" && classInfo.PreviousClassEndTime != classInfo.StartTime && classInfo.NextClassStartTime > DateTime.Now)
                    {
                        availableMinutes = TimeGapBetweenMeetings(classInfo.NextClassStartTime, classInfo.EndTime);
                    }
                }
            }
            return availableMinutes > 15 ? "Visible" : "Hidden";
        }

        private int TimeGapBetweenMeetings(DateTime maxTime, DateTime minTime)
        {
            return System.Convert.ToInt32((maxTime - minTime).TotalMinutes);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
