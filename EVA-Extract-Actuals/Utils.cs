using System;
using System.Linq;

namespace EVA_Extract_Actuals
{
    public static class Utils
    {
        /// https://stackoverflow.com/questions/1617049/calculate-the-number-of-business-days-between-two-dates
        /// <summary>
        /// Calculates number of business days, taking into account:
        ///  - weekends (Saturdays and Sundays)
        /// </summary>
        /// <param name="firstDay">First day in the time interval</param>
        /// <param name="lastDay">Last day in the time interval</param>
        /// <returns>Number of business days during the 'span'</returns>
        public static int GetBusinessDays(this DateTime firstDay, DateTime lastDay)
        {
            firstDay = firstDay.Date;
            lastDay = lastDay.Date;
            if (firstDay > lastDay)
                throw new ArgumentException("Incorrect last day " + lastDay);

            TimeSpan span = lastDay - firstDay;
            int businessDays = span.Days + 1;
            int fullWeekCount = businessDays / 7;
            // Find out if there are weekends during the time exceedng the full weeks
            if (businessDays > fullWeekCount * 7)
            {
                // We are here to find out if there is a 1-day or 2-days weekend
                // in the time interval remaining after subtracting the complete weeks
                int firstDayOfWeek = firstDay.DayOfWeek == DayOfWeek.Sunday
                    ? 7 : (int)firstDay.DayOfWeek;
                int lastDayOfWeek = lastDay.DayOfWeek == DayOfWeek.Sunday
                    ? 7 : (int)lastDay.DayOfWeek;
                if (lastDayOfWeek < firstDayOfWeek)
                    lastDayOfWeek += 7;
                if (firstDayOfWeek <= 6)
                {
                    if (lastDayOfWeek >= 7)// Both Saturday and Sunday are in the remaining time interval
                        businessDays -= 2;
                    else if (lastDayOfWeek >= 6)// Only Saturday is in the remaining time interval
                        businessDays -= 1;
                }
                else if (firstDayOfWeek <= 7 && lastDayOfWeek >= 7)// Only Sunday is in the remaining time interval
                    businessDays -= 1;
            }

            // Subtract the weekends during the full weeks in the interval
            businessDays -= fullWeekCount + fullWeekCount;

            return businessDays;
        }

        /// <summary>
        /// Calculates number of periods between the two dates, inclusive.
        /// </summary>
        /// <param name="date1">Larger day in the time interval</param>
        /// <param name="date2">Smaller day in the time interval</param>
        /// <returns>Number of periods during the 'span'</returns>
        public static int GetTotalPeriods(DateTime date1, DateTime date2)
        {
            return Math.Abs(((date1.Year - date2.Year) * 12) + date1.Month - date2.Month) + 1;
        }

        /// <summary>
        /// Iterates over child tasks and  child task packages, updating the minimum period
        /// found in each iteration.
        /// </summary>
        /// <param name="taskPackage">Task Package that we want to search</param>
        /// <returns>The minimum period found</returns>
        public static int GetMinPeriodOfChildren(TaskPackage taskPackage)
        {
            int minStartPeriod = Int32.MaxValue;

            if (taskPackage.TaskPackages.Count > 0)
            {
                foreach (var tp in taskPackage.TaskPackages)
                {
                    if (tp.PlannedProgress.Count > 0)
                    {
                        minStartPeriod = Math.Min(minStartPeriod, tp.PlannedProgress.Select(pp => pp.Period).Min());
                    }
                }
            }

            if (taskPackage.Tasks.Count > 0)
            {
                foreach (var t in taskPackage.Tasks)
                {
                    if (t.PlannedProgress.Count > 0)
                    {
                        minStartPeriod = Math.Min(minStartPeriod, t.PlannedProgress.Select(pp => pp.Period).Min());
                    }
                }
            }

            return minStartPeriod == Int32.MaxValue ? 0 : minStartPeriod;
        }

        /// <summary>
        /// Iterates over child tasks and child task packages, updating the maximum period
        /// found in each iteration.
        /// </summary>
        /// <param name="taskPackage">Task Package that we want to search</param>
        /// <returns>The maximum period found</returns>
        public static int GetMaxPeriodOfChildren(TaskPackage taskPackage)
        {
            int maxStartPeriod = 0;

            if (taskPackage.TaskPackages.Count > 0)
            {
                foreach (var tp in taskPackage.TaskPackages)
                {
                    if (tp.PlannedProgress.Count > 0)
                    {
                        maxStartPeriod = Math.Max(maxStartPeriod, tp.PlannedProgress.Select(pp => pp.Period).Max());
                    }
                }
            }

            if (taskPackage.Tasks.Count > 0)
            {
                foreach (var t in taskPackage.Tasks)
                {
                    if (t.PlannedProgress.Count > 0)
                    {
                        maxStartPeriod = Math.Max(maxStartPeriod, t.PlannedProgress.Select(pp => pp.Period).Max());
                    }
                }
            }

            return maxStartPeriod;
        }

        /// <summary>
        /// Iterates over child tasks, summing together their planned duration 
        /// in each pass. Task Package duration is calculated recursively
        /// by summing together the duration of its own child tasks.
        /// </summary>
        /// <param name="parentTaskPackage">Task Package that we use to search</param>
        /// <returns>The total duration of all children</returns>
        public static int GetChildrenTaskPackageDuration(TaskPackage parentTaskPackage)
        {
            var totalDuration = 0;
            foreach (var task in parentTaskPackage.Tasks)
            {
                //feels dirty doing this here, but trying to insert validation at every place it
                //could get corrupted is problematic
                int taskDuration = 0;
                if (int.TryParse(task.PlannedDuration, out taskDuration))
                {
                    totalDuration += taskDuration;
                }
                else
                {
                    DateTime startDate = DateTime.Parse(task.PlannedStartDate);
                    DateTime endDate = DateTime.Parse(task.PlannedEndDate);
                    task.PlannedDuration = Utils.GetBusinessDays(startDate, endDate).ToString();
                    totalDuration += Int32.Parse(task.PlannedDuration);
                }

            }

            foreach (var taskPackage in parentTaskPackage.TaskPackages)
            {
                totalDuration += GetChildrenTaskPackageDuration(taskPackage);
            }
            return totalDuration;
        }

        /// <summary>
        /// Iterates over child tasks and child task packages, summing together 
        /// their planned cost in each pass.
        /// </summary>
        /// <param name="parentTaskPackage">Task Package that we use to search</param>
        /// <returns>The total cost of all children</returns>
        public static decimal GetChildrenTaskPackageCost(TaskPackage parentTaskPackage)
        {
            var totalCost = 0M;
            foreach (var task in parentTaskPackage.Tasks)
            {
                totalCost += task.PlannedCost ?? 0;
            }

            foreach (var taskPackage in parentTaskPackage.TaskPackages)
            {
                totalCost += taskPackage.PlannedCost ?? 0;
            }
            return totalCost;
        }

        /// <summary>
        /// Gets the weight of the cost or duration and multiplies it with its
        /// allocation value. Used for calculating the progress of parent task package
        /// </summary>
        /// <param name="project">Project model used to get root task package</param>
        /// <param name="parentTaskPackage">Task Package that we want to update</param>
        /// <param name="child">Child Task we want to get weight from</param>
        /// <param name="totalDuration">Total sum of duration from all children</param>
        /// <param name="totalCost">Total sum of costs from all children</param>
        /// <returns>The cost-duration multiplier for the specific task</returns>
        public static decimal GetTaskWeightedCostDurationMultiplier(EVAProject project, TaskPackage parentTaskPackage, Task child, decimal totalDuration, decimal totalCost)
        {
            decimal multiplier = 0M;
            decimal durationWeight = totalDuration == 0 ? 0M : Decimal.Parse(child.PlannedDuration) / totalDuration;
            decimal durationWeightAllocation = Decimal.Parse(parentTaskPackage.GetDurationWeightAllocation(project.RootTaskPackage)) / 100m;
            multiplier += durationWeight * durationWeightAllocation;

            decimal costWeight = totalCost == 0 ? 0M : (child.PlannedCost ?? 0) / totalCost;
            decimal costWeightAllocation = Decimal.Parse(parentTaskPackage.GetCostWeightAllocation(project.RootTaskPackage)) / 100m;
            multiplier += costWeight * costWeightAllocation;

            return multiplier;
        }

        /// <summary>
        /// Gets the weight of the cost or duration and multiplies it with its
        /// allocation value. Used for calculating the progress of parent task package
        /// </summary>
        /// <param name="project">Project model used to get root task package</param>
        /// <param name="parentTaskPackage">Task Package that we want to update</param>
        /// <param name="child">Child Task we want to get weight from</param>
        /// <param name="totalDuration">Total sum of duration from all children</param>
        /// <param name="totalCost">Total sum of costs from all children</param>
        /// <returns>The cost-duration multiplier for the specific task</returns>
        public static decimal GetTaskPackageWeightedCostDurationMultiplier(EVAProject project, TaskPackage parentTaskPackage, TaskPackage child, decimal totalDuration, decimal totalCost)
        {
            decimal multiplier = 0M;
            decimal durationWeight = totalDuration == 0 ? 0M : (decimal)GetChildrenTaskPackageDuration(child) / totalDuration;
            decimal durationWeightAllocation = Decimal.Parse(parentTaskPackage.GetDurationWeightAllocation(project.RootTaskPackage)) / 100m;
            multiplier += durationWeight * durationWeightAllocation;

            decimal costWeight = totalCost == 0 ? 0M : (child.PlannedCost ?? 0) / totalCost;
            decimal costWeightAllocation = Decimal.Parse(parentTaskPackage.GetCostWeightAllocation(project.RootTaskPackage)) / 100m;
            multiplier += costWeight * costWeightAllocation;

            return multiplier;
        }

        /// <summary>
        /// Calculates end date with the aid of start date and duration that is known.
        /// </summary>
        /// <param name="startDateStr">The start day in the time interval</param>
        /// <param name="duration">No. of day in the time interval</param>
        /// <returns> Returns the end date in the time interval</returns>
        public static string GetEndDate(string startDateStr, string duration)
        {
            //DateOnly startDate = DateOnly.Parse(startDateStr);
            DateTime startDate = DateTime.Parse(startDateStr).ToUniversalTime();
            int complWeeks = (int.Parse(duration)) / 5;
            int totalDuration = (complWeeks * 2) + int.Parse(duration);
            var endDate = startDate.AddDays(totalDuration);

            var endDateStr = endDate.ToString();
            return endDate.ToString();
        }

        public static DateTime AddBusinessDays(DateTime date, int days)
{
    if (days < 0)
    {
        throw new ArgumentException("days cannot be negative", "days");
    }

    if (days == 0) return date;

    if (date.DayOfWeek == DayOfWeek.Saturday)
    {
        date = date.AddDays(2);
        days -= 1;
    }
    else if (date.DayOfWeek == DayOfWeek.Sunday)
    {
        date = date.AddDays(1);
        days -= 1;
    }

    date = date.AddDays(days / 5 * 7);
    int extraDays = days % 5;

    if ((int)date.DayOfWeek + extraDays > 5)
    {
        extraDays += 2;
    }

    return date.AddDays(extraDays);

}
    }
}