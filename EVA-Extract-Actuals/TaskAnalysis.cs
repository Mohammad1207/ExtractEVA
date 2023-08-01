using Newtonsoft.Json;
using System;

namespace EVA_Extract_Actuals
{
    
    public class TaskAnalysis : Analysis
    {

        private decimal? _earnedSchedule = null;

        private decimal? _schedulePerformanceIndexTime = null;

        [JsonIgnore]
        private Task _task;

        public TaskAnalysis(int period, string projectsForecastMethod, decimal projectsFACValue, Task task)
        {
            Period = period;
            ProjectsForecastMethod = projectsForecastMethod;
            ProjectsFACValue = projectsFACValue;
            _task = task;
        }

        public bool IsPreBase { get { return false; } }

        public int Period { get; set; }
        public string ProjectsForecastMethod { get; set; }
        public decimal ProjectsFACValue { get; set; }

        public decimal? ActualCost
        {
            get
            {
                return _task.FindActual(this.Period).ActualCost;
            }
        }

        public decimal? ActualCostPeriod
        {
            get
            {
                return _task.FindPeriodActual(this.Period).ActualCost;
            }
        }

        public decimal? ActualProgress
        {
            get
            {
                return (_task.FindActual(this.Period).Progress ?? 0M) / 100M;
            }
        }

        public decimal? BudgetAtCompletion
        {
            get
            {
                return _task.PlannedCost;
            }
        }

        public decimal? BudgetSpent
        {
            get
            {
                if (this.BudgetAtCompletion > 0M)
                {
                    return this.ActualCost / this.BudgetAtCompletion;
                }
                else
                {
                    return null;
                }

            }
        }

        public decimal? CostPerformanceIndex
        {
            get
            {
                if (this.ActualCost > 0M)
                {
                    return this.EarnedValue / this.ActualCost;
                }
                else
                {
                    return null;
                }

            }
        }

        public decimal? CostPerformanceIndexPeriod
        {
            get
            {
                if (this.ActualCostPeriod == 0M)
                {
                    return 0M;
                }
                else
                {
                    return this.EarnedValuePeriod / this.ActualCostPeriod;
                }

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
                        var earnedValue = this.EarnedValue;
                        var prev = this._task.FindAnalysis(this.Period - 1, false);
                        var start = prev.EarnedValue > this.EarnedValue ? 0 : (int)prev.EarnedSchedule;

                        for (var period = start; period <= this._task.PlannedEndPeriod; ++period)
                        {
                            var plannedValueNextPeriod = this._task.FindAnalysis(period + 1, false).PlannedValue;

                            if (earnedValue <= plannedValueNextPeriod)
                            {
                                var plannedValueThisPeriod = this._task.FindAnalysis(period, false).PlannedValue;

                                if (plannedValueThisPeriod < plannedValueNextPeriod)
                                {
                                    this._earnedSchedule = period + ((earnedValue - plannedValueThisPeriod) / (plannedValueNextPeriod - plannedValueThisPeriod));
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        this._earnedSchedule = this._task.PlannedEndPeriod;
                    }

                    // Adjust earned schedule so that it is relative to the planned start period.
                    this._earnedSchedule -= this._task.PlannedStartPeriod - 1;
                }

                return this._earnedSchedule ?? 0;
            }
        }

        // Gets the earned schedule for the period.
        public virtual decimal? EarnedSchedulePeriod
        {
            get
            {
                var prevEarnedSchedule = this._task.FindAnalysis(this.Period - 1, false)?.EarnedSchedule ?? 0M;
                return this.EarnedSchedule - prevEarnedSchedule;
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
                var prevEarnedValue = this._task.FindAnalysis(this.Period - 1, false)?.EarnedValue ?? 0M;
                return this.EarnedValue - prevEarnedValue;
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
        public decimal? ForecastAtCompletionMean
        {
            get
            {
                if (_task.ForecastMethod == "Default")
                {
                    if (this.ProjectsForecastMethod == "BudgetRate")
                    {
                        return (decimal)this.BudgetedRateForecast;
                    }
                    else if (this.ProjectsForecastMethod == "PastCostPerformance")
                    {
                        return (decimal)this.PastCostPerformanceForecast;
                    }
                    else if (this.ProjectsForecastMethod == "PastSchedulePerformace")
                    {
                        return (decimal)this.PastSchedulePerformanceForecast;
                    }
                    else if (this.ProjectsForecastMethod == "ScheduleAndCostIndexed")
                    {
                        return (decimal)this.ScheduleAndCostIndexedForecast;
                    }
                    else if (this.ProjectsForecastMethod == "BudgetAtCompletion")
                    {
                        return (decimal)this.BudgetAtCompletion;
                    }
                    else if (this.ProjectsForecastMethod == "Constant")
                    {
                        return this.ProjectsFACValue;
                    }
                    else { return 0M; }
                }
                else if (_task.ForecastMethod == "BudgetRate")
                {
                    return (decimal)this.BudgetedRateForecast;
                }
                else if (_task.ForecastMethod == "PastCostPerformance")
                {
                    return (decimal)this.PastCostPerformanceForecast;
                }
                else if (_task.ForecastMethod == "PastSchedulePerformace")
                {
                    return (decimal)this.PastSchedulePerformanceForecast;
                }
                else if (_task.ForecastMethod == "ScheduleAndCostIndexed")
                {
                    return (decimal)this.ScheduleAndCostIndexedForecast;
                }
                else if (_task.ForecastMethod == "BudgetAtCompletion")
                {
                    return (decimal)this.BudgetAtCompletion;
                }
                else if (_task.ForecastMethod == "Constant")
                {
                    return (decimal)Int64.Parse(this._task.ConstantForcastValue);
                }
                else { return 0M; }
            }
        }

        // Gets the standard deviation of the forecasted cost of the work task at completion.
        public decimal? ForecastAtCompletionStDev { get; } = 1M;


        // Gets a comment about the actual cost.
        public virtual string Note
        {
            get
            {
                return _task.FindPeriodActual(this.Period).Note;
            }
        }


        // Gets the cumulative planned progress.
        public decimal? PlannedProgress
        {
            get
            {
                return _task.FindPlan(this.Period).ExpectedProgress / 100M;
            }
        }

        // Gets the cumulative planned value.
        public decimal? PlannedValue
        {
            get
            {
                return _task.FindPlan(this.Period).ExpectedCost;
            }
        }

        // Gets the planned value for the period.
        public virtual decimal? PlannedValuePeriod
        {
            get
            {
                var prevPlannedValue = this._task.FindAnalysis(this.Period - 1, false)?.PlannedValue ?? 0M;
                return this.PlannedValue - prevPlannedValue;
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
                    return 1M + (this.ActualProgress);
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
                    decimal actualProgressPeriod = (this._task.FindPeriodActual(this.Period).Progress / 100M) ?? 0M;
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
                    var prev = _task.FindAnalysis(this.Period - 1, false);

                    if (this.EarnedValue < this.BudgetAtCompletion || (prev?.EarnedValue ?? 0M) < this.BudgetAtCompletion)
                    {
                        if (this.TaskPeriod > 0)
                        {
                            _schedulePerformanceIndexTime = this.EarnedSchedule / this.TaskPeriod;
                        }
                        else
                        {
                            _schedulePerformanceIndexTime = 1M + (this.ActualProgress);
                        }
                    }
                    else
                    {
                        _schedulePerformanceIndexTime = prev?.SchedulePerformanceIndexTime ?? 0M;
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
                return this.Period - (this._task.PlannedStartPeriod ?? 0) + 1;
            }
        }
    }

}