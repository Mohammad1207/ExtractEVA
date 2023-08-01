using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EVA_Extract_Actuals
{
    public class TaskPackageAnalysis : Analysis
    {

        private decimal? _earnedSchedule = null;

        private decimal? _schedulePerformanceIndexTime = null;

        [JsonIgnore]
        private TaskPackage _taskPackage;

        public TaskPackageAnalysis(int period, TaskPackage taskPackage)
        {
            Period = period;
            _taskPackage = taskPackage;
        }

        public bool IsPreBase { get { return false; } }
        public int Period { get; set; }

        public decimal? ActualCost
        {
            get
            {
                return _taskPackage.FindActual(this.Period).ActualCost ?? 0M;
            }
        }

        public decimal? ActualCostPeriod
        {
            get
            {
                return _taskPackage.FindPeriodActual(this.Period).ActualCost ?? 0M;
            }
        }

        public decimal? ActualProgress
        {
            get
            {
                return (_taskPackage.FindActual(this.Period).Progress ?? 0M) / 100M;
            }
        }

        public decimal? BudgetAtCompletion
        {
            get
            {
                return _taskPackage.PlannedCost ?? 0M;
            }
        }

        public decimal? BudgetSpent
        {
            get
            {
                if (this.BudgetAtCompletion == 0)
                {
                    return 0;
                }
                else
                {
                    return this.ActualCost / this.BudgetAtCompletion;
                }

            }
        }

        public decimal? CostPerformanceIndex
        {
            get
            {
                return this.ActualCost == 0 ? 0 : this.EarnedValue / this.ActualCost;
            }
        }

        public decimal? CostPerformanceIndexPeriod
        {
            get
            {

                return this.ActualCostPeriod == 0 ? 0 : this.EarnedValuePeriod / this.ActualCostPeriod;
            }
        }

        public decimal? CostVariance
        {
            get
            {
                return this.EarnedValue - this.ActualCost;
            }
        }

        public virtual decimal? EarnedSchedule
        {
            get
            {
                return 0;
                if (this.ActualProgress == 0M)
                {
                    return 0M;
                }
                else if (!this._earnedSchedule.HasValue)
                {
                    if (this.EarnedValue == this.PlannedValue)
                    {
                        this._earnedSchedule = this.Period;
                    }
                    else if (this.EarnedValue < this.BudgetAtCompletion)
                    {
                        var x = this.EarnedValue;
                        var prev = this._taskPackage.FindAnalysis(this.Period - 1, false);
                        var start = prev.EarnedValue > this.EarnedValue ? 0 : (int)prev.EarnedSchedule;

                        for (var period = start; period <= this._taskPackage.PlannedEndPeriod; ++period)
                        {
                            var x1 = this._taskPackage.FindAnalysis(period + 1, false).PlannedValue;

                            if (x <= x1)
                            {
                                var x0 = this._taskPackage.FindAnalysis(period, false).PlannedValue;

                                if (x0 < x1)
                                {
                                    var y0 = period;
                                    var y1 = y0 + 1M;

                                    this._earnedSchedule = y0 + ((y1 - y0) * (x - x0) / (x1 - x0));
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        this._earnedSchedule = this._taskPackage.PlannedEndPeriod;
                    }

                    // Adjust earned schedule so that it is relative to the planned start period.
                    this._earnedSchedule -= this._taskPackage.PlannedStartPeriod - 1;
                }

                return this._earnedSchedule ?? 0;
            }
        }

        // Gets the earned schedule for the period.
        public virtual decimal? EarnedSchedulePeriod
        {
            get
            {
                var prev = this._taskPackage.FindAnalysis(this.Period - 1, false);
                return this.EarnedSchedule - prev.EarnedSchedule;
            }
        }

        // Gets the cumulative earned value.
        public virtual decimal? EarnedValue
        {
            get
            {
                return this.BudgetAtCompletion * (decimal)this.ActualProgress;
            }
        }

        // Gets the earned value for the period.
        public virtual decimal? EarnedValuePeriod
        {
            get
            {
                var prev = this._taskPackage.FindAnalysis(this.Period - 1, false);
                return this.EarnedValue - prev.EarnedValue;
            }
        }


        // Gets the forecasted cost of the work task at completion by FAC = AC + BAC - EV.
        public virtual decimal? BudgetedRateForecast
        {
            get
            {
                var ac = this.ActualCost;
                var ap = this.ActualProgress;
                var bac = this.BudgetAtCompletion;
                var ev = bac * (decimal)ap;
                return ac + bac - ev;
            }
        }

        // Gets the forecasted cost of the work task at completion by FAC = BAC / CPI
        public decimal? PastCostPerformanceForecast
        {
            get
            {
                var ac = this.ActualCost;
                var ap = this.ActualProgress;
                var bac = this.BudgetAtCompletion;
                var ev = bac * (decimal)ap;
                return ev != 0M ? bac * ac / ev : 0M;
            }
        }

        // Gets the forecasted cost of the work task at completion by FAC = AC + (BAC - EV) / (CPI x SPI).
        public decimal? PastSchedulePerformanceForecast
        {
            get
            {
                var ac = this.ActualCost;
                var ap = this.ActualProgress;
                var bac = this.BudgetAtCompletion;
                var ev = bac * (decimal)ap;
                var cpi = ac > 0 ? ev / ac : 0M;
                var spi = this.SchedulePerformanceIndexTime;
                var denominator = cpi * spi;

                if (denominator != 0M)
                {
                    return ac + ((bac - ev) / denominator);
                }
                else
                {
                    return 0M;
                }
            }
        }

        // Gets the forecasted cost of the work task at completion by FAC = FAC = AC + (BAC - EV) / (weighted CPI  +  weighted SPI).
        public decimal? ScheduleAndCostIndexedForecast
        {
            get
            {

                var ac = this.ActualCost;
                var ap = this.ActualProgress;
                var bac = this.BudgetAtCompletion;
                var ev = bac * (decimal)ap;
                var cpi = ac > 0 ? ev / ac : 0M;
                var spi = this.SchedulePerformanceIndexTime;
                var denominator = (0.5M * cpi) + (0.5M * spi);

                if (denominator != 0M)
                {
                    return ac + ((bac - ev) / (decimal)denominator);
                }
                else
                {
                    return 0M;
                }
            }
        }
        // Gets the mean of the forecasted cost of the work task at completion.
        public decimal? ForecastAtCompletionMean { get; } = 1M;

        // Gets the standard deviation of the forecasted cost of the work task at completion.
        public decimal? ForecastAtCompletionStDev { get; } = 1M;


        // Gets a comment about the actual cost.
        public virtual string Note
        {
            get
            {
                return _taskPackage.FindPeriodActual(this.Period).Note;
            }
        }


        // Gets the cumulative planned progress.
        public decimal? PlannedProgress
        {
            get
            {
                return _taskPackage.FindPlan(this.Period).ExpectedProgress;
            }
        }

        // Gets the cumulative planned value.
        public decimal? PlannedValue
        {
            get
            {
                return _taskPackage.FindPlan(this.Period).ExpectedCost;
            }
        }

        // Gets the planned value for the period.
        public virtual decimal? PlannedValuePeriod
        {
            get
            {
                var prev = this._taskPackage.FindAnalysis(this.Period - 1, false);
                return this.PlannedValue - prev.PlannedValue;
            }
        }

        // Gets the cumulative cost-based schedule performance index.
        public virtual decimal? SchedulePerformanceIndexCost
        {
            get
            {
                if (this.PlannedValue > 0M)
                {
                    return this.EarnedValue / this.PlannedValue;
                }
                else
                {
                    return 1M + this.ActualProgress;
                }
            }
        }


        // Gets the cost-based schedule performance index for the period.
        public virtual decimal? SchedulePerformanceIndexCostPeriod
        {
            get
            {
                if (this.PlannedValuePeriod > 0M)
                {
                    return this.EarnedValuePeriod / this.PlannedValuePeriod;
                }
                else
                {
                    decimal actualProgressPeriod = this._taskPackage.FindPeriodActual(this.Period).Progress ?? 0M;
                    return 1M + actualProgressPeriod;
                }
            }
        }

        // Gets the cumulative time-based schedule performance index.
        public virtual decimal? SchedulePerformanceIndexTime
        {
            get
            {
                if (!_schedulePerformanceIndexTime.HasValue)
                {
                    var prev = _taskPackage.FindAnalysis(this.Period - 1, false);

                    if (this.EarnedValue < this.BudgetAtCompletion || prev.EarnedValue < this.BudgetAtCompletion)
                    {
                        if (this.TaskPeriod > 0)
                        {
                            _schedulePerformanceIndexTime = this.EarnedSchedule / this.TaskPeriod;
                        }
                        else
                        {
                            _schedulePerformanceIndexTime = 1M + this.ActualProgress;
                        }
                    }
                    else
                    {
                        _schedulePerformanceIndexTime = prev.SchedulePerformanceIndexTime;
                    }
                }

                return this._schedulePerformanceIndexTime.Value;
            }
        }

        // Gets the time-based schedule performance index for the period.
        public virtual decimal? SchedulePerformanceIndexTimePeriod
        {
            get
            {
                return this.EarnedSchedulePeriod;
            }
        }

        // Gets the cumulative cost-based schedule variance.
        public decimal? ScheduleVarianceCost
        {
            get
            {
                return this.EarnedValue - this.PlannedValue;
            }
        }


        // Gets the cumulative time-based schedule variance.
        public decimal? ScheduleVarianceTime
        {
            get
            {
                return this.EarnedSchedule - this.TaskPeriod;
            }
        }


        // Gets the mean of the variance between the budgeted and forecasted costs of the work task
        public decimal? VarianceAtCompletionMean
        {
            get
            {
                return this.BudgetAtCompletion - this.ForecastAtCompletionMean;
            }
        }

        // Gets the period number relative to the planned start period.
        private int TaskPeriod
        {
            get
            {
                return this.Period - (this._taskPackage.PlannedStartPeriod ?? 0) + 1;
            }
        }
    }

}