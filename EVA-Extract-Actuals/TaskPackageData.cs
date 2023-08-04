using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVA_Extract_Actuals
{
    public class TaskPackage : TaskBase
    {

        private decimal? _plannedCost;
        private decimal? _actualCost;
        private List<PlannedProgressEntry> _plannedProgress;
        private List<ActualProgressEntry> _actualProgress;
        public string CostWeightAllocation;
        public string DurationWeightAllocation;

        [BsonIgnore]
        public override bool IsPackage { get; } = true;

        public override string PlannedStartDate
        {
            get => AllTasks.Select(taskBase => taskBase.PlannedStartDate).Min();
            set { } //discard rather than throw exception so client submitting a value to be deserialized isn't considered an error
        }

        public override string PlannedEndDate
        {
            get => AllTasks.Select(taskBase => taskBase.PlannedEndDate).Max();
            set { }
        }

        public override decimal? PlannedCost
        {
            get
            {
                if (_plannedCost == null)
                {
                    _plannedCost = Tasks.Select(task => task.PlannedCost).Sum() + TaskPackages.Select(package => package.PlannedCost).Sum();
                }
                return _plannedCost;
            }
            set
            {
            }
        }
        internal override int? PlannedStartPeriod
        {
            get
            {
                return AllTasks.Select(entry => entry.PlannedStartPeriod).Min();
            }
        }

        internal override int? PlannedEndPeriod
        {
            get
            {
                return AllTasks.Select(entry => entry.PlannedEndPeriod).Max();
            }
        }

        public decimal? ActualCost
        {
            get
            {
                if (_actualCost == null)
                {
                    _actualCost = Tasks.Select(task => task.ActualProgress.Sum(actual => actual.ActualCost ?? 0M)).Sum() + TaskPackages.Select(package => package.ActualCost).Sum();
                }

                return _actualCost;
            }
            set
            {
            }
        }

        public List<string> TaskIds { get; set; } = new List<string>();

        [BsonIgnore]
        public List<Task> Tasks { get; set; } = new List<Task>();

        internal IEnumerable<Task> AllTasks
        {
            get
            {
                var allTasks = new List<Task>(Tasks);
                foreach (var package in TaskPackages)
                {
                    allTasks.AddRange(package.AllTasks);
                }
                return allTasks;
            }
        }

        public List<TaskPackage> TaskPackages { get; set; } = new List<TaskPackage>();

        [BsonIgnore]
        public override List<ActualProgressEntry> ActualProgress
        {
            get
            {
                if (_actualProgress == null || _actualProgress.Count == 0)
                {
                    _actualProgress = CalculateActualProgress();
                }
                return new List<ActualProgressEntry>(_actualProgress); //don't let callers mutate our cache.
            }
            set
            {
            }
        }

        [BsonIgnore]
        public override List<PlannedProgressEntry> PlannedProgress
        {
            get
            {
                if (_plannedProgress == null || _plannedProgress.Count == 0)
                {
                    _plannedProgress = CalculatePlannedProgress();
                }
                return new List<PlannedProgressEntry>(_plannedProgress); //don't let callers mutate our cache.
            }
            set
            {
            }
        }

        internal override int? MaxReportedPeriod
        {
            get
            {
                return Math.Max(Tasks.Select(task => task.MaxReportedPeriod).Max() ?? 0, TaskPackages.Select(package => package.MaxReportedPeriod).Max() ?? 0);
            }
        }

        internal override int? MinReportedPeriod
        {
            get
            {
                return Math.Min(Tasks.Select(task => task.MinReportedPeriod).Min() ?? 0, TaskPackages.Select(package => package.MinReportedPeriod).Min() ?? 0);
            }
        }

        private List<ActualProgressEntry> CalculateActualProgress()
        {
            var result = new List<ActualProgressEntry>();

            var plannedCost = this.PlannedCost ?? 0M;

            var startPeriod = Math.Min(this.Tasks.Select(task => task.MinReportedPeriod).Min() ?? 0, this.TaskPackages.Select(package => package.MinReportedPeriod).Min() ?? 0);
            var endPeriod = Math.Max(this.Tasks.Select(task => task.MaxReportedPeriod).Max() ?? 0, this.TaskPackages.Select(package => package.MaxReportedPeriod).Max() ?? 0);

            for (var period = startPeriod; period <= endPeriod; period++)
            {
                var progress = 0M;
                var actualCost = 0M;
                var anticipatedCost = 0M;
                foreach (var task in Tasks)
                {
                    var actual = task.FindPeriodActual(period);
                    if (actual != null)
                    {
                        var taskActualCost = actual.ActualCost ?? 0;
                        var taskAnticipatedCost = actual.AnticipatedCost ?? 0;
                        actualCost += taskActualCost;
                        if (taskActualCost == 0)
                        {
                            anticipatedCost += taskAnticipatedCost;
                        }
                        progress += (task.PlannedCost ?? 0M) * (actual.Progress ?? 0M);
                    }
                }
                foreach (var taskPackage in TaskPackages)
                {
                    var actual = taskPackage.FindPeriodActual(period);
                    if (actual != null)
                    {
                        var taskActualCost = actual.ActualCost ?? 0;
                        var taskAnticipatedCost = actual.AnticipatedCost ?? 0;
                        actualCost += taskActualCost;
                        if (taskActualCost == 0)
                        {
                            anticipatedCost += taskAnticipatedCost;
                        }
                        progress += (taskPackage.PlannedCost ?? 0M) * (actual.Progress ?? 0M);
                    }
                }

                if (plannedCost != 0)
                {
                    progress /= plannedCost;
                }
                else
                {
                    progress = 0M;
                }

                var periodActual = new ActualProgressEntry()
                {
                    Period = period,
                    ActualCost = actualCost,
                    Progress = progress,
                    AnticipatedCost = anticipatedCost
                };
                result.Add(periodActual);
            }

            return result;
        }

        private List<PlannedProgressEntry> CalculatePlannedProgress()
        {
            var result = new List<PlannedProgressEntry>();

            var plannedCost = this.PlannedCost ?? 0M;

            var startPeriod = this.AllTasks.Select(task => task.PlannedStartPeriod).Min() ?? 0;
            var endPeriod = this.AllTasks.Select(task => task.PlannedEndPeriod).Max() ?? 0;

            for (var period = startPeriod; period <= endPeriod; period++)
            {
                var progress = 0M;
                var plannedPeriodCost = 0M;
                foreach (var task in Tasks)
                {
                    var plan = task.FindPeriodPlan(period);
                    if (plan != null)
                    {
                        var taskPlannedCost = plan.ExpectedCost;
                        plannedPeriodCost += taskPlannedCost;
                        progress += (task.PlannedCost ?? 0M) * plan.ExpectedProgress;
                    }
                }
                foreach (var taskPackage in TaskPackages)
                {
                    var plan = taskPackage.FindPeriodPlan(period);
                    if (plan != null)
                    {
                        var taskActualCost = plan.ExpectedCost;
                        plannedPeriodCost += taskActualCost;
                        progress += (taskPackage.PlannedCost ?? 0M) * plan.ExpectedProgress;
                    }
                }

                if (plannedCost != 0)
                {
                    progress /= plannedCost;
                }
                else
                {
                    progress = 0M;
                }

                var periodPlan = new PlannedProgressEntry()
                {
                    Period = period,
                    ExpectedCost = plannedPeriodCost,
                    ExpectedProgress = progress,
                };
                result.Add(periodPlan);
            }
            return result;
        }
        
    }
}
