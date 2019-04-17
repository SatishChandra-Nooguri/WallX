using System;
using System.Collections.Generic;
using System.Linq;

namespace WallX.Services
{
    class CustomDatesFrequency
    {
        public static List<DateTime> GetDailyDates(DateTime startDate, DateTime endDate)
        {
            List<DateTime> dates = new List<DateTime>();
            dates.Add(startDate);
            while ((startDate = startDate.AddDays(1)) <= endDate)
            {
                dates.Add(startDate);
            }
            return dates;
        }

        public static List<DateTime> GetAlternateDates(DateTime startDate, DateTime endDate)
        {
            try
            {
                List<DateTime> dates = new List<DateTime>();
                int i = 1;
                dates.Add(startDate);
                while ((startDate = startDate.AddDays(1)) <= endDate)
                {
                    if (i % 2 == 0)
                        dates.Add(startDate);
                    i++;
                }
                return dates; 
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<DateTime> GetWeekDates(DateTime startDate, DateTime endDate)
        {
            try
            {
                List<DateTime> dates = new List<DateTime>();
                int i = 1;
                dates.Add(startDate);
                while ((startDate = startDate.AddDays(1)) <= endDate)
                {
                    if (i % 7 == 0)
                        dates.Add(startDate);
                    i++;
                }
                return dates;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static List<DateTime> GetMonthlyDates(DateTime startDate, DateTime endDate)
        {
            try
            {
                List<DateTime> dates = new List<DateTime>();
                List<DateTime> datesMonthly = new List<DateTime>();
                int currentDay = startDate.Day;
                int date = startDate.Day;
                dates.Add(startDate);
                while ((startDate = startDate.AddDays(1)) <= endDate)
                {
                    dates.Add(startDate);
                }
                List<DateTime> finalDatesList = new List<DateTime>();
                dates.GroupBy(s => s.Year).Distinct().Select(s => s.GroupBy(k => k.Month).Distinct().Select(l => l.FirstOrDefault())).ToList().ForEach(s => finalDatesList.AddRange(s.ToList()));
                foreach (DateTime currentdate in finalDatesList)
                {
                    if (IsDateValid(currentdate.Year, currentdate.Month, currentDay))
                    {
                        date = currentDay;
                    }
                    else if (IsDateValid(currentdate.Year, currentdate.Month, currentDay - 1))
                    {
                        date = currentDay - 1;
                    }
                    else if (IsDateValid(currentdate.Year, currentdate.Month, currentDay - 2))
                    {
                        date = currentDay - 2;
                    }
                    else if (IsDateValid(currentdate.Year, currentdate.Month, currentDay - 3))
                    {
                        date = currentDay - 3;
                    }
                    DateTime dateTime = Convert.ToDateTime(string.Format("{0}/{1}/{2}", date, currentdate.Month, currentdate.Year));
                    if (dateTime <= endDate)
                        datesMonthly.Add(dateTime);
                }
                return datesMonthly;
            }
            catch (Exception)
            {
                return null;
            }
        }

        static bool IsDateValid(int year, int month, int day)
        {
            return day <= DateTime.DaysInMonth(year, month);
        }

        public static List<DateTime> GetYearlyDates(DateTime startDate, DateTime endDate)
        {
            try
            {
                List<DateTime> dates = new List<DateTime>();
                DateTime date = startDate;
                int startYear = startDate.Year;
                int endYear = endDate.Year;

                if (startDate.Month <= endDate.Month && startDate.Day <= endDate.Day)
                {
                }
                else
                {
                    endYear = endYear - 1;
                }

                if (date.Month == 02 && date.Day == 29)
                {
                    for (int j = startYear; j <= endYear; j++)
                    {
                        if ((j % 4 == 0 && j % 100 != 0) || (j % 400 == 0))
                        {
                            dates.Add(Convert.ToDateTime(string.Format("{0}/{1}/{2}", date.Day, date.Month, j)));
                        }
                        else
                        {
                            dates.Add(Convert.ToDateTime(string.Format("{0}/{1}/{2}", date.Day - 1, date.Month, j)));
                        }
                    }
                }
                else
                {
                    for (int j = startYear; j <= endYear; j++)
                    {
                        dates.Add(Convert.ToDateTime(string.Format("{0}/{1}/{2}", date.Day, date.Month, j)));
                    }
                }
                return dates;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
