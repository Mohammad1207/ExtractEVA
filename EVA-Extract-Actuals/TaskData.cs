using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA_Extract_Actuals
{

    public class Task : TaskBase
    {
        public DistributionType Distribution { get; set; } = new DistributionType();


        internal override void ReDefineProjTaskID(string parent)
        {
            var unqId = ObjectId.GenerateNewId().ToString();
            this.Id = unqId;
            this.Parent = parent;
        }

        internal override void InitializeAnalysis(int period = 0, string projectsForecastMethod = "", decimal projectsFACValue = 0M)
        {
            _analysis = new List<Analysis>();

            if (period > 0)
            {
                foreach (var plan in PlannedProgress)
                {
                    if (plan.Period <= period)
                    {
                        _analysis.Add(new TaskAnalysis(plan.Period, projectsForecastMethod, projectsFACValue, this));
                    }
                    else
                    {
                        _analysis.Add(new ZeroAnalysis(plan.Period, this));
                    }
                }
            }

            else
            {
                foreach (var plan in PlannedProgress)
                {
                    _analysis.Add(new TaskAnalysis(plan.Period, projectsForecastMethod, projectsFACValue, this));
                }
            }

        }

        internal override void InitAnalysisPreBaseline(List<PreBaselineTaskBase> combinedPreBaselineArr)
        {
            // Extracts the prebaseline information for a task based on it's Id, from a list of Depth First Traversed prebaseline informations
            var preBaselineInfo = combinedPreBaselineArr.Find(p => p.SourceId == this.Id);
            if (preBaselineInfo != null)
            {

                // To have data for all the existing planned period and progress of task packages which has already been baselined, it replaces those active calculation with the Historical Data 
                foreach (var plan in PlannedProgress)
                {
                    var historicalPreBases = preBaselineInfo.HistoricalBaselineData;
                    var historicalPreBase = historicalPreBases.Find(h => h.Period == plan.Period);

                    // Checks if the Historical Data exists for a specific period, if it does, then it gets substituted by the return of "TaskPackageAnalysisPreBase"
                    if (historicalPreBase != null)
                    {
                        var index = this.Analysis.FindIndex(a => a.Period == plan.Period);
                        Analysis[index] = new TaskAnalysisPreBase(historicalPreBase);
                    }
                }

            }

        }

        public PreBaselineTask StoreTAnalysis(int period)
        {
            PreBaselineTask preBaselineT = new PreBaselineTask
            {
                Code = this.Code,
                PreBaselinePeriod = period,
                SourceId = this.Id,
                Parent = this.Parent,
                Name = this.Name,
                PlannedStartDate = this.PlannedStartDate,
                PlannedEndDate = this.PlannedEndDate,
                PlannedCost = this.PlannedCost ?? 0,
                PlannedDuration = this.PlannedDuration
            };

            HistoricalBaselineData baselineData = new HistoricalBaselineData();
            for (var i = 0; i < Analysis.Count(); i++)
            {
                if (Analysis[i].Period <= period)
                {
                    baselineData.Period = Analysis[i].Period;
                    baselineData.PeriodWeight = PlannedProgress[i].PeriodWeight;
                    baselineData.HistoricalActualCost = Analysis[i].ActualCost ?? 0M;
                    baselineData.HistoricalActualCost_P = Analysis[i].ActualCostPeriod ?? 0M;
                    baselineData.HistoricalActualProgress = Analysis[i].ActualProgress ?? 0M;
                    baselineData.HistoricalBAC = Analysis[i].BudgetAtCompletion ?? 0M;
                    baselineData.HistoricalBSpent = Analysis[i].BudgetSpent ?? 0M;
                    baselineData.HistoricalCPI = Analysis[i].CostPerformanceIndex ?? 0M;
                    baselineData.HistoricalCPI_P = Analysis[i].CostPerformanceIndexPeriod ?? 0M;
                    baselineData.HistoricalCostVariance = Analysis[i].CostVariance ?? 0M;
                    baselineData.HistoricalEarnedSchedule = Analysis[i].EarnedSchedule ?? 0M;
                    baselineData.HistoricalEarnedSchedule_P = Analysis[i].EarnedSchedulePeriod ?? 0M;
                    baselineData.HistoricalEarnedValue = Analysis[i].EarnedValue ?? 0M;
                    baselineData.HistoricalEarnedValue_P = Analysis[i].EarnedValuePeriod ?? 0M;
                    baselineData.HistoricalBudgetedRateForecast = Analysis[i].BudgetedRateForecast ?? 0M;
                    baselineData.HistoricalPastCostPerformanceForecast = Analysis[i].PastCostPerformanceForecast ?? 0M;
                    baselineData.HistoricalPastSchedulePerformanceForecast = Analysis[i].PastSchedulePerformanceForecast ?? 0M;
                    baselineData.HistoricalScheduleAndCostIndexedForecast = Analysis[i].ScheduleAndCostIndexedForecast ?? 0M;
                    baselineData.HistoricalFAC_Mean = Analysis[i].ForecastAtCompletionMean ?? 0M;
                    baselineData.HistoricalFAC_SD = Analysis[i].ForecastAtCompletionStDev ?? 0M;
                    baselineData.HistoricalPlannedProgress = Analysis[i].PlannedProgress ?? 0M;
                    baselineData.HistoricalPlannedCost = Analysis[i].PlannedValue ?? 0M;
                    baselineData.HistoricalPlannedCost_P = Analysis[i].PlannedValuePeriod ?? 0M;
                    baselineData.HistoricalSPI_C = Analysis[i].SchedulePerformanceIndexCost ?? 0M;
                    baselineData.HistoricalSPI_CP = Analysis[i].SchedulePerformanceIndexCostPeriod ?? 0M;
                    baselineData.HistoricalSPI_T = Analysis[i].SchedulePerformanceIndexTime ?? 0M;
                    baselineData.HistoricalSPI_TP = Analysis[i].SchedulePerformanceIndexTimePeriod ?? 0M;
                    baselineData.HistoricalScheduleVariance_C = Analysis[i].ScheduleVarianceCost ?? 0M;
                    baselineData.HistoricalScheduleVariance_T = Analysis[i].ScheduleVarianceTime ?? 0M;
                    baselineData.HistoricalVAC = Analysis[i].VarianceAtCompletionMean ?? 0M;
                    baselineData.HistoricalNote = Analysis[i].Note;

                    preBaselineT.HistoricalBaselineData.Add(baselineData);

                    baselineData = new HistoricalBaselineData();
                }
            }

            return preBaselineT;
        }
    }

}
