using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace EVA_Extract_Actuals
{

    public class ZeroAnalysis : Analysis
    {

        [JsonIgnore]
        private Task _task;
        private TaskPackage _taskPackage;


        public ZeroAnalysis(int period, Task task)
        {
            Period = period;
            _task = task;
        }

        public ZeroAnalysis(int period, TaskPackage taskPackage)
        {
            Period = period;
            _taskPackage = taskPackage;
        }


        public ZeroAnalysis(int period)
        {
            Period = period;
        }


        public bool IsPreBase { get; } = false;
        public int Period { get; set; }

        public decimal? ActualCost { get; } = 0M;

        public decimal? ActualCostPeriod { get; } = 0M;

        public decimal? ActualProgress { get; } = 0M;

        public decimal? BudgetAtCompletion { get; } = 0M;

        public decimal? BudgetSpent { get; } = 0M;

        public decimal? CostPerformanceIndex { get; } = 0M;

        public decimal? CostPerformanceIndexPeriod { get; } = 0M;

        public decimal? CostVariance { get; } = 0M;

        public decimal? EarnedSchedule { get; } = 0M;

        // Gets the earned schedule for the period.
        public decimal? EarnedSchedulePeriod { get; } = 0M;

        // Gets the cumulative earned value.
        public decimal? EarnedValue { get; } = 0M;

        // Gets the earned value for the period.
        public decimal? EarnedValuePeriod { get; } = 0M;

        // Gets the mean of the forecasted cost of the work task at completion.
        public decimal? BudgetedRateForecast { get; } = 0M;

        // Gets the mean of the forecasted cost of the work task at completion.
        public decimal? PastCostPerformanceForecast { get; } = 0M;

        // Gets the mean of the forecasted cost of the work task at completion.
        public decimal? PastSchedulePerformanceForecast { get; } = 0M;

        // Gets the mean of the forecasted cost of the work task at completion.
        public decimal? ScheduleAndCostIndexedForecast { get; } = 0M;

        // Gets the mean of the forecasted cost of the work task at completion.
        public decimal? ForecastAtCompletionMean { get; } = 0M;

        // Gets the standard deviation of the forecasted cost of the work task at completion.
        public decimal? ForecastAtCompletionStDev { get; } = 0M;


        // Gets a comment about the actual cost.
        public string Note { get; }

        // Gets the cumulative planned progress.
        public decimal? PlannedProgress { get; } = 0M;

        // Gets the cumulative planned value.
        public decimal? PlannedValue { get; } = 0M;

        // Gets the planned value for the period.
        public decimal? PlannedValuePeriod { get; } = 0M;

        // Gets the cumulative cost-based schedule performance index.
        public decimal? SchedulePerformanceIndexCost { get; } = 0M;

        // Gets the cost-based schedule performance index for the period.
        public decimal? SchedulePerformanceIndexCostPeriod { get; } = 0M;

        // Gets the cumulative time-based schedule performance index.
        public decimal? SchedulePerformanceIndexTime { get; } = 0M;

        public decimal? SchedulePerformanceIndexTimePeriod { get; } = 0M;

        public decimal? ScheduleVarianceCost { get; } = 0M;

        // Gets the cumulative time-based schedule variance.
        public decimal? ScheduleVarianceTime { get; } = 0M;

        // Gets the mean of the variance between the budgeted and forecasted costs of the work task
        public decimal? VarianceAtCompletionMean { get; } = 0M;
    }
}