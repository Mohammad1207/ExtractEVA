
namespace EVA_Extract_Actuals
{

    public interface Analysis
    {
        bool IsPreBase { get; }

        int Period { get; set; }

        decimal? ActualCost { get; }

        decimal? ActualCostPeriod { get; }

        decimal? ActualProgress { get; }

        decimal? BudgetAtCompletion { get; }

        decimal? BudgetSpent { get; }

        decimal? CostPerformanceIndex { get; }

        decimal? CostPerformanceIndexPeriod { get; }

        decimal? CostVariance { get; }

        decimal? EarnedSchedule { get; }

        // Gets the earned schedule for the period.
        decimal? EarnedSchedulePeriod { get; }

        // Gets the cumulative earned value.
        decimal? EarnedValue { get; }

        // Gets the earned value for the period.
        decimal? EarnedValuePeriod { get; }

        // Gets the forecasted cost of the work task at completion by FAC = AC + BAC - EV.
        decimal? BudgetedRateForecast { get; }

        // Gets the forecasted cost of the work task at completion by FAC = BAC / CPI
        decimal? PastCostPerformanceForecast { get; }

        // Gets the forecasted cost of the work task at completion by FAC = AC + (BAC - EV) / (CPI x SPI).
        decimal? PastSchedulePerformanceForecast { get; }

        // Gets the forecasted cost of the work task at completion by FAC = FAC = AC + (BAC - EV) / (weighted CPI  +  weighted SPI).
        decimal? ScheduleAndCostIndexedForecast { get; }

        // Gets the mean of the forecasted cost of the work task at completion.
        decimal? ForecastAtCompletionMean { get; }

        // Gets the standard deviation of the forecasted cost of the work task at completion.
        decimal? ForecastAtCompletionStDev { get; }


        // Gets a comment about the actual cost.
        string Note { get; }

        // Gets the cumulative planned progress.
        decimal? PlannedProgress { get; }

        // Gets the cumulative planned value.
        decimal? PlannedValue { get; }

        // Gets the planned value for the period.
        decimal? PlannedValuePeriod { get; }

        // Gets the cumulative cost-based schedule performance index.
        decimal? SchedulePerformanceIndexCost { get; }

        // Gets the cost-based schedule performance index for the period.
        decimal? SchedulePerformanceIndexCostPeriod { get; }

        // Gets the cumulative time-based schedule performance index.
        decimal? SchedulePerformanceIndexTime { get; }

        decimal? SchedulePerformanceIndexTimePeriod { get; }

        decimal? ScheduleVarianceCost { get; }

        // Gets the cumulative time-based schedule variance.
        decimal? ScheduleVarianceTime { get; }

        // Gets the mean of the variance between the budgeted and forecasted costs of the work task
        decimal? VarianceAtCompletionMean { get; }
    }
}