using EVA_Extract_Actuals;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVA_Extract_Actuals
{
    internal static class PlanBuilder
    {

        public static Task CalculatePlan(Task task)
        {
            var distributionCode = task.Distribution.Code;
            if (DistributionGenerators.Generators.ContainsKey(distributionCode))
            {
                task.PlannedProgress = BuildLibraryDistribution(task, distributionCode);
            }
            else
            {

                var EvaDB = Database.GetDatabase();
                var collection = EvaDB.GetCollection<CustomDistribution>(Database.Collections.DISTRIBUTIONS);
                var filter = Builders<CustomDistribution>.Filter.Eq(d => d.Distribution, distributionCode);
                var distribution = collection.Find(filter);
                var entity = distribution.SingleOrDefault();
                decimal[] data = Array.ConvertAll(entity.Distribution.Split(";"), r => decimal.Parse(r));

                task.PlannedProgress = BuildDistribution(task, distributionCode, data);
            }

            return task;
        }

        internal static List<PlannedProgressEntry> BuildLibraryDistribution(Task task, string distributionCode)
        {
            bool taskCustomDistribution = distributionCode == "Custom";
            int len = task.PlannedProgress != null ? task.PlannedProgress.Count() : 0;
            decimal[] distribution = new decimal[len];
            var workingPlanLength = task.PlannedProgress.Where(entry => !entry.NonWorkingPeriod).Count();
            var historicalBaselineData = task.Baselines.SelectMany(baseline => baseline.HistoricalBaselineData).ToList();
            var percentComplete = historicalBaselineData.Aggregate(0M, (acc, cum) => acc + cum.HistoricalActualProgress);

            if (distributionCode != "Custom")
            {
                distribution = new decimal[workingPlanLength];
                distribution = DistributionGenerators.Generators[distributionCode](workingPlanLength, percentComplete);
            }
            else
            {
                int size = task.PlannedProgress.Count();
                distribution = new decimal[size];

                for (int i = 0; i < distribution.Count(); i++)
                {
                    distribution[i] = task.PlannedProgress[i].ExpectedProgress;
                }
            }

            return ApplyDistributionToTaskWithActuals(task, distribution, taskCustomDistribution);
        }

        internal static List<PlannedProgressEntry> BuildDistribution(Task task, string distributionCode, decimal[] customDistribution)
        {
            decimal[] distribution = new decimal[task.PlannedProgress.Count()];
            var workingPlanLength = task.PlannedProgress.Where(entry => !entry.NonWorkingPeriod).Count();

            var historicalBaselineData = task.Baselines.SelectMany(baseline => baseline.HistoricalBaselineData).ToList();
            var percentCompleted = historicalBaselineData.Aggregate(0M, (acc, cum) => acc + cum.HistoricalActualProgress);

            // Get distribution up until new starting point and 
            decimal complete = 0;
            var index = 0;
            while (complete < percentCompleted)
            {
                complete += customDistribution[index];
                index++;
            }

            var modifiedCustomDistribution = customDistribution.Skip(index).ToArray();

            var sum = modifiedCustomDistribution.Sum();
            modifiedCustomDistribution = modifiedCustomDistribution.Select(num => num / sum).ToArray();
            distribution = DistributionGenerators.ResizeDistribution(workingPlanLength, modifiedCustomDistribution);

            return ApplyDistributionToTaskWithActuals(task, distribution, false);
        }

        private static List<PlannedProgressEntry> ApplyDistributionToTaskWithActuals(Task task, decimal[] distribution, bool taskCustomDistribution)
        {
            List<PlannedProgressEntry> actualResults = new List<PlannedProgressEntry>();
            List<PlannedProgressEntry> results = new List<PlannedProgressEntry>();
            // var workingPlanLength = task.PlannedProgress.Where(entry => !entry.NonWorkingPeriod).Count();
            var totalPlanLength = task.PlannedProgress.Count();
            // var distributionSize = distribution.Count();
            var currentWorkingIncrement = 0;
            var historicalBaselineData = task.Baselines.SelectMany(baseline => baseline.HistoricalBaselineData).ToList();

            for (var i = 0; i < totalPlanLength; i++)
            {
                PlannedProgressEntry newEntry = new PlannedProgressEntry();
                newEntry.Period = task.PlannedProgress[i].Period;
                newEntry.PeriodWeight = task.PlannedProgress[i].PeriodWeight;
                if (currentWorkingIncrement < historicalBaselineData.Count && newEntry.Period == historicalBaselineData[currentWorkingIncrement].Period)
                {

                    newEntry.ExpectedProgress = historicalBaselineData[currentWorkingIncrement].HistoricalActualProgress;
                    newEntry.ExpectedCost = historicalBaselineData[currentWorkingIncrement].HistoricalActualCost;
                    currentWorkingIncrement++;
                    newEntry.NonWorkingPeriod = task.PlannedProgress[i].NonWorkingPeriod;

                    actualResults.Add(newEntry);
                }
                else
                {
                    if (taskCustomDistribution)
                    {
                        newEntry.ExpectedProgress = decimal.Parse((distribution[currentWorkingIncrement]).ToString("F3"));
                        newEntry.ExpectedCost = decimal.Parse((distribution[currentWorkingIncrement] * task.PlannedCost ?? 0M).ToString("F0"));
                        currentWorkingIncrement++;
                        newEntry.NonWorkingPeriod = false;
                    }
                    else
                    {
                        if (task.PlannedProgress[i].NonWorkingPeriod)
                        {
                            newEntry.ExpectedProgress = 0M;
                            newEntry.ExpectedCost = 0M;
                            newEntry.NonWorkingPeriod = true;
                        }
                        else
                        {
                            newEntry.ExpectedProgress = decimal.Parse((distribution[currentWorkingIncrement] * 100M).ToString("F3"));
                            newEntry.ExpectedCost = decimal.Parse((distribution[currentWorkingIncrement] * task.PlannedCost ?? 0M).ToString("F0"));
                            currentWorkingIncrement++;
                            newEntry.NonWorkingPeriod = false;
                        }
                    }

                    results.Add(newEntry);
                }
            }

            if (!taskCustomDistribution)
            {
                var ActualCosts = historicalBaselineData.Select(data => data.HistoricalActualCost).Aggregate(0M, (acc, cum) => acc + cum);
                var ActualProgress = historicalBaselineData.Select(data => data.HistoricalActualProgress).Aggregate(0M, (acc, cum) => acc + cum);
                ApplyPeriodWeightsWithActuals(results, task.PlannedCost ?? 0M, ActualCosts, ActualProgress);
            }

            return actualResults.Concat(results).ToList();
        }

        //As implied by the void return this method mutates the input.
        //Scales the total progress expectation to sum to 100 after accounting for the progress that has already passed.
        //total expected value is required to re-scale the weighted values back to the planned value
        private static void ApplyPeriodWeightsWithActuals(List<PlannedProgressEntry> unweightedEntries, decimal totalExpectedValue, decimal actualCosts, decimal actualProgress)
        {
            var totalExpectedProgress = 0M;
            var totalScaledCost = 0M;
            var workingPeriods = unweightedEntries.Where(entry => !entry.NonWorkingPeriod);
            foreach (var progressPlan in workingPeriods)
            {
                progressPlan.ExpectedProgress = progressPlan.ExpectedProgress * progressPlan.PeriodWeight;
                progressPlan.ExpectedCost = progressPlan.ExpectedCost * progressPlan.PeriodWeight;
                totalExpectedProgress += progressPlan.ExpectedProgress;
                totalScaledCost += progressPlan.ExpectedCost;
            }
            var progressScaleFactor = (100M - actualProgress) / totalExpectedProgress;
            var costScaleFactor = (totalExpectedValue - actualCosts) / (totalScaledCost > 0M ? totalScaledCost : 1M);

            foreach (var progressPlan in workingPeriods)
            {
                progressPlan.ExpectedProgress = Decimal.Round(progressPlan.ExpectedProgress * progressScaleFactor, 1, MidpointRounding.ToEven);
                progressPlan.ExpectedCost = Decimal.Round(progressPlan.ExpectedCost * costScaleFactor, 2, MidpointRounding.ToEven);
            }

            /* var workingPeriods = unweightedEntries.Where(entry => !entry.NonWorkingPeriod);
            var partialWorkingPeriods = workingPeriods.Where(entry => entry.PeriodWeight < 1.0M);
            var fullWorkingPeriods = workingPeriods.Where(entry => entry.PeriodWeight > 0.99M);
            var totalDaysInAllPeriods = workingPeriods.Count() * 22M;
            var totalEffortInFullPeriods = fullWorkingPeriods.Select(entry => entry.ExpectedProgress).Sum();
            var totalRemainderEffortInPartialPeriods = 0M;
            //scale down partial periods.
            foreach (var partialPeriod in partialWorkingPeriods)
            {
              totalRemainderEffortInPartialPeriods += partialPeriod.ExpectedProgress  - (partialPeriod.ExpectedProgress * partialPeriod.PeriodWeight);
              partialPeriod.ExpectedCost = partialPeriod.ExpectedCost * partialPeriod.PeriodWeight;
              partialPeriod.ExpectedProgress = partialPeriod.ExpectedProgress * partialPeriod.PeriodWeight;
            }
            var scaleFactor = 1M + (totalRemainderEffortInPartialPeriods / totalEffortInFullPeriods);

            foreach (var fullPeriod in fullWorkingPeriods) 
            {
              fullPeriod.ExpectedProgress = fullPeriod.ExpectedProgress * scaleFactor;
            } */
        }
    }
}
