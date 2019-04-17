using System;
using System.Windows.Data;
using System.Windows;
using System.Collections.Generic;
using WallX.Services;
using WallX.Helpers;

namespace WallX.Converters
{
    public class AddClassIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int availableMinutes = 0;
            if (value != null && value is Class)
            {
                Class classInfo = (Class)value;
                string preMeetEndTime = classInfo.PreviousClassEndTime.ToString("hh:mm tt");
                string nextMeetEndTime = classInfo.NextClassStartTime.ToString("hh:mm tt");

                if (classInfo.StartTime.Date >= DateTime.Now.Date)
                {
                    if (parameter.ToString() == "icon_PLUS_Carosel_first" && preMeetEndTime == Constants.DayStartTime && classInfo.StartTime > DateTime.Now)
                    {
                        availableMinutes = TimeGapBetweenClass(classInfo.StartTime, classInfo.PreviousClassEndTime);
                    }
                    else if (parameter.ToString() == "icon_PLUS_Carosel_first" && preMeetEndTime == Constants.DayStartTime && classInfo.StartTime > DateTime.Now)
                    {
                        availableMinutes = TimeGapBetweenClass(classInfo.StartTime, DateTime.Now);
                    }
                    else if (parameter.ToString() == "icon_PLUS_Carosel_last" && nextMeetEndTime == Constants.DayEndTime)
                    {
                        availableMinutes = TimeGapBetweenClass(classInfo.NextClassStartTime, classInfo.EndTime);
                    }
                    else if (parameter.ToString() == "icon_PLUS_Carosel_right" && classInfo.StartTime > DateTime.Now &&
                        preMeetEndTime != Constants.DayStartTime && classInfo.PreviousClassEndTime != classInfo.StartTime &&
                        (classInfo.StartTime > DateTime.Now || DateTime.Now.Subtract(classInfo.EndTime).TotalMinutes < 0) &&
                        classInfo.StartTime > classInfo.PreviousClassEndTime)
                    {
                        availableMinutes = TimeGapBetweenClass(classInfo.StartTime, classInfo.PreviousClassEndTime);
                    }
                    else if (parameter.ToString() == "icon_PLUS_Carosel_left" && classInfo.EndTime != classInfo.NextClassStartTime && classInfo.NextClassStartTime > DateTime.Now)
                    {
                        availableMinutes = TimeGapBetweenClass(classInfo.NextClassStartTime, classInfo.EndTime);
                    }
                    else if (new List<string> { "canv_Reshedule_Event", "canv_Delete_Event", "canv_Play", "canv_Preview" }.Contains(parameter.ToString()) && classInfo.StartTime.ToString("yyyy-MM-dd") == DateTime.Now.ToString("yyyy-MM-dd") &&
                        DateTime.Now.Subtract(classInfo.EndTime).TotalMinutes <= 0 && DateTime.Now.Subtract(classInfo.PreviousClassEndTime).TotalMinutes > 0)
                    {
                        availableMinutes = 30;
                    }
                }
            }
            return availableMinutes > 15 ? Visibility.Visible : Visibility.Hidden;
        }

        private int TimeGapBetweenClass(DateTime maxTime, DateTime minTime)
        {
            return System.Convert.ToInt32((maxTime - minTime).TotalMinutes);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
