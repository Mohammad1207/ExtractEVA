using System;
using System.Linq;

namespace EVA_Extract_Actuals
{
    public static class Utils
    {
        /// <summary>
        /// Calculates number of periods between the two dates, inclusive.
        /// </summary>
        /// <param name="date1">Larger day in the time interval</param>
        /// <param name="date2">Smaller day in the time interval</param>
        /// <returns>Number of periods during the 'span'</returns>
        public static int GetTotalPeriods(DateOnly date1, DateOnly date2)
        {
            return Math.Abs(((date1.Year - date2.Year) * 12) + date1.Month - date2.Month) + 1;
        }

        /// <summary>
        /// Calculates end date with the aid of start date and duration that is known.
        /// </summary>
        /// <param name="startDateStr">The start day in the interval</param>
        /// <param name="duration">No. of day in the time interval</param>
        /// <returns> Returns the end date in the interval</returns>
        public static string GetEndDate(string startDateStr, string duration)
        {
            DateOnly startDate = DateOnly.Parse(startDateStr);
            int complWeeks = (int.Parse(duration)) / 5;
            int totalDuration = (complWeeks * 2) + int.Parse(duration);
            var endDate = startDate.AddDays(totalDuration);

            return endDate.ToString();
        }  
    }
}