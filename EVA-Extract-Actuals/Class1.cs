/*using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace EVA_Extract_Actuals
{
    public static class EVATaskManagement
    {
        public static Task RecalculatePeriods(Task task)
        {
            DateTime startDate = DateTime.Parse(task.PlannedStartDate);
            DateTime endDate = DateTime.Parse(task.PlannedEndDate);
            task.PlannedDuration = Utils.GetBusinessDays(startDate, endDate).ToString();
            var periods = Utils.GetTotalPeriods(startDate, endDate);
            //task.RecalculateEVAPeriods(inputProject.GetPeriodMap());
            task.CalculatePlan();

            return task;
        }

    }
}*/