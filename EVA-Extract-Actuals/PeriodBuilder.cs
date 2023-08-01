using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
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
                    var duration = child.GetAttribute("PlannedDuration");
                    var startDate = child.GetAttribute("PlannedStartDate");
                    var endDate = Utils.GetEndDate(startDate, duration);

                    DurationDates newDurationDates = new DurationDates();
                    newDurationDates.StartDate = startDate;
                    newDurationDates.EndDate = endDate;
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
		internal static DateTime GetEndOfMonth(this DateTime date)
        {
            var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            var endOfMonth = new DateTime(date.Year, date.Month, daysInMonth, 23, 59, 59);
            return endOfMonth;
        }

        public static Dictionary<DateTime, int> GetPeriodMap(string statDateStr, string endDateStr)
		{
			DateTime projectStart = DateTime.Parse(statDateStr).ToUniversalTime();
			DateTime projectEnd = DateTime.Parse(endDateStr).ToUniversalTime();

			var numPeriods = Utils.GetTotalPeriods(projectEnd, projectStart);
			Dictionary<DateTime, int> periodsMap = new Dictionary<DateTime, int>();
			var currYear = projectStart.Year;
			var currMonth = projectStart.Month;
			for (var i = 0; i < numPeriods; i++)
			{
				var currStartPeriod = new DateTime(currYear, currMonth, 1);
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

        public static decimal GetPeriodWeight(int period, int startPeriod, int endPeriod, DateTime startMonth, DateTime endMonth, Task task)
        {
            var plannedStartDateTime = DateTime.Parse(task.PlannedStartDate).ToUniversalTime();
            var plannedEndDateTime = DateTime.Parse(task.PlannedEndDate).ToUniversalTime();
            var result = 0M;
            if (startPeriod == endPeriod)
            {
                result = 1M;
            }
            else if (period == startPeriod)
            {
                var endOfStartMonth = plannedStartDateTime.GetEndOfMonth();
                result = (GetWorkingDays(plannedStartDateTime, endOfStartMonth) / 23M);
            }
            else if (period == endPeriod)
            {
                result = (GetWorkingDays(endMonth, plannedEndDateTime) / 23M);
            }
            else
            {
                var periodMonth = startMonth.AddMonths(period - startPeriod);
                var periodMonthEnd = periodMonth.GetEndOfMonth();
                result = (GetWorkingDays(periodMonth, periodMonthEnd) / 23M);
            }

            return result;
        }

        private static decimal GetWorkingDays(DateTime start, DateTime end)
        {
            double calcBusinessDays =
                1 + ((end - start).TotalDays * 5 -
                (start.DayOfWeek - end.DayOfWeek) * 2) / 7;

            if (end.DayOfWeek == DayOfWeek.Saturday) calcBusinessDays--;
            if (start.DayOfWeek == DayOfWeek.Sunday) calcBusinessDays--;

            return (decimal)calcBusinessDays;
        }

    }
}
