using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml;

namespace EVA_Extract_Actuals
{
	public static class PeriodBuilder
	{

        public static List<DurationDates> GetStartAndEndDateList(List<DurationDates> durationDates, XmlNodeList children)
        {
            foreach (XmlElement child in children)
            {
                var objectTypeString = child.GetAttribute("AssemblyQualifiedName");

                if (objectTypeString.Contains("WorkPackage"))
                {
                    XmlNodeList packageChildren = child.SelectNodes("Children/Child");
                    durationDates = GetStartAndEndDateList(durationDates, packageChildren);
                }
                else if (objectTypeString.Contains("WorkTask"))
                {
                    string duration = child.GetAttribute("PlannedDuration");
                    DateTime startDateTime = DateTime.Parse(child.GetAttribute("PlannedStartDate"));
                    DateOnly startDate = DateOnly.FromDateTime(startDateTime);
                    string startDateStr = startDate.ToString();
                    string endDateStr = Utils.GetEndDate(startDateStr, duration);

                    DurationDates newDurationDates = new()
                    {
                        StartDate = startDateStr,
                        EndDate = endDateStr
                    };
                    durationDates.Add(newDurationDates);
                }
            }
            return durationDates;
        }

        /// <summary>
		/// Return a new DateTime that corresponds to the last second of the month that date falls in.
		/// </summary>
		/// <param name="date">Extension target.</param>
		/// <returns>A new DateTime object set to the end of the month.</returns>
		internal static DateOnly GetEndOfMonth(this DateOnly date)
        {
            var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            var endOfMonth = new DateOnly(date.Year, date.Month, daysInMonth);
            return endOfMonth;
        }

        public static Dictionary<DateOnly, int> GetPeriodMap(string statDateStr, string endDateStr)
		{
			DateOnly projectStart = DateOnly.Parse(statDateStr);
            DateOnly projectEnd = DateOnly.Parse(endDateStr);

            var numPeriods = Utils.GetTotalPeriods(projectEnd, projectStart);
			Dictionary<DateOnly, int> periodsMap = new Dictionary<DateOnly, int>();
			var currYear = projectStart.Year;
			var currMonth = projectStart.Month;
			for (var i = 0; i < numPeriods; i++)
			{
				var currStartPeriod = new DateOnly(currYear, currMonth, 1);
				periodsMap.Add(currStartPeriod, i + 1);

				currMonth++;
				if (currMonth > 12)
				{
					currYear++;
					currMonth = 1;
				}
			}
			return periodsMap;
		}

        public static decimal GetPeriodWeight(int period, int startPeriod, int endPeriod, DateOnly startMonth, DateOnly endMonth, Task task)
        {
            var plannedStartDate = DateOnly.Parse(task.PlannedStartDate);
            var plannedEndDate = DateOnly.Parse(task.PlannedEndDate);
            var result = 0M;
            
            if (startPeriod == endPeriod)
            {
                result = 1M;
            }
            else if (period == startPeriod)
            {
                var endOfStartMonth = plannedStartDate.GetEndOfMonth();
                result = (GetWorkingDays(plannedStartDate, endOfStartMonth) / 23M);
            }
            else if (period == endPeriod)
            {
                result = (GetWorkingDays(endMonth, plannedEndDate) / 23M);
            }
            else
            {
                var periodMonth = startMonth.AddMonths(period - startPeriod);
                var periodMonthEnd = periodMonth.GetEndOfMonth();
                result = (GetWorkingDays(periodMonth, periodMonthEnd) / 23M);
            }

            return result;
        }

        private static decimal GetWorkingDays(DateOnly start, DateOnly end)
        {
            double calcBusinessDays =
                1 + ((end.DayNumber - start.DayNumber) * 5 -
                (start.DayOfWeek - end.DayOfWeek) * 2) / 7;

            if (end.DayOfWeek == DayOfWeek.Saturday) calcBusinessDays--;
            if (start.DayOfWeek == DayOfWeek.Sunday) calcBusinessDays--;

            return (decimal)calcBusinessDays;
        }

    }
}
