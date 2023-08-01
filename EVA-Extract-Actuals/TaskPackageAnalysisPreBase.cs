using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EVA_Extract_Actuals
{
    public class TaskPackageAnalysisPreBase : Analysis
    {

        [JsonIgnore]
        private HistoricalBaselineData _historicalPreBase;

        public TaskPackageAnalysisPreBase(HistoricalBaselineData historicalPreBase)
        {
            Period = historicalPreBase.Period;
            _historicalPreBase = historicalPreBase;
        }
        public bool IsPreBase { get { return true; } }

        public int Period { get; set; }

        public decimal? ActualCost
        {
            get { return this._historicalPreBase.HistoricalActualCost; }
        }

        public decimal? ActualCostPeriod
        {
            get { return this._historicalPreBase.HistoricalActualCost_P; }
        }

        public decimal? ActualProgress
        {
            get { return this._historicalPreBase.HistoricalActualProgress; }
        }

        public decimal? BudgetAtCompletion
        {
            get { return this._historicalPreBase.HistoricalBAC; }
        }

        public decimal? BudgetSpent
        {
            get { return this._historicalPreBase.HistoricalBSpent; }
        }

        public decimal? CostPerformanceIndex
        {
            get { return this._historicalPreBase.HistoricalCPI; }
        }

        public decimal? CostPerformanceIndexPeriod
        {
            get { return this._historicalPreBase.HistoricalCPI_P; }
        }

        public decimal? CostVariance
        {
            get { return this._historicalPreBase.HistoricalCostVariance; }
        }

        public virtual decimal? EarnedSchedule
        {
            get { return this._historicalPreBase.HistoricalEarnedSchedule; }
        }

        // Gets the earned schedule for the period.
        public virtual decimal? EarnedSchedulePeriod
        {
            get { return this._historicalPreBase.HistoricalEarnedSchedule_P; }
        }

        // Gets the cumulative earned value.
        public virtual decimal? EarnedValue
        {
            get { return this._historicalPreBase.HistoricalEarnedValue; }
        }

        // Gets the earned value for the period.
        public virtual decimal? EarnedValuePeriod
        {
            get { return this._historicalPreBase.HistoricalEarnedValue_P; }
        }

        // Gets the forecasted cost of the work task at completion by FAC = AC + BAC - EV.
        public virtual decimal? BudgetedRateForecast
        {
            get { return this._historicalPreBase.HistoricalBudgetedRateForecast; }
        }

        // Gets the forecasted cost of the work task at completion by FAC = BAC / CPI
        public decimal? PastCostPerformanceForecast
        {
            get { return this._historicalPreBase.HistoricalPastCostPerformanceForecast; }
        }

        // Gets the forecasted cost of the work task at completion by FAC = AC + (BAC - EV) / (CPI x SPI).
        public decimal? PastSchedulePerformanceForecast
        {
            get { return this._historicalPreBase.HistoricalPastSchedulePerformanceForecast; }
        }

        // Gets the forecasted cost of the work task at completion by FAC = FAC = AC + (BAC - EV) / (weighted CPI  +  weighted SPI).
        public decimal? ScheduleAndCostIndexedForecast
        {
            get { return this._historicalPreBase.HistoricalScheduleAndCostIndexedForecast; }
        }

        // Gets the mean of the forecasted cost of the work task at completion.
        public decimal? ForecastAtCompletionMean
        {
            get { return this._historicalPreBase.HistoricalFAC_Mean; }
        }

        // Gets the standard deviation of the forecasted cost of the work task at completion.
        public decimal? ForecastAtCompletionStDev
        {
            get { return this._historicalPreBase.HistoricalFAC_SD; }
        }

        // Gets a comment about the actual cost.
        public virtual string Note
        {
            get { return this._historicalPreBase.HistoricalNote; }
        }

        // Gets the cumulative planned progress.
        public decimal? PlannedProgress
        {
            get { return this._historicalPreBase.HistoricalPlannedProgress; }
        }

        // Gets the cumulative planned value.
        public decimal? PlannedValue
        {
            get { return this._historicalPreBase.HistoricalPlannedCost; }
        }

        // Gets the planned value for the period.
        public virtual decimal? PlannedValuePeriod
        {
            get { return this._historicalPreBase.HistoricalPlannedCost_P; }
        }

        // Gets the cumulative cost-based schedule performance index.
        public virtual decimal? SchedulePerformanceIndexCost
        {
            get { return this._historicalPreBase.HistoricalSPI_C; }
        }


        // Gets the cost-based schedule performance index for the period.
        public virtual decimal? SchedulePerformanceIndexCostPeriod
        {
            get { return this._historicalPreBase.HistoricalSPI_CP; }
        }

        // Gets the cumulative time-based schedule performance index.
        public virtual decimal? SchedulePerformanceIndexTime
        {
            get { return this._historicalPreBase.HistoricalSPI_T; }
        }

        // Gets the time-based schedule performance index for the period.
        public virtual decimal? SchedulePerformanceIndexTimePeriod
        {
            get { return this.EarnedSchedulePeriod; }
        }

        // Gets the cumulative cost-based schedule variance.
        public decimal? ScheduleVarianceCost
        {
            get { return this._historicalPreBase.HistoricalScheduleVariance_C; }
        }

        // Gets the cumulative time-based schedule variance.
        public decimal? ScheduleVarianceTime
        {
            get { return this._historicalPreBase.HistoricalScheduleVariance_T; }
        }

        // Gets the mean of the variance between the budgeted and forecasted costs of the work task
        public decimal? VarianceAtCompletionMean
        {
            get { return this._historicalPreBase.HistoricalVAC; }
        }

    }

}