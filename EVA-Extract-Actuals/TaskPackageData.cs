using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

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

        internal override void ReDefineProjTaskID(string parent)
        {
            var unqId = ObjectId.GenerateNewId().ToString();
            this.Id = unqId;
            this.Parent = parent;

            foreach (var package in TaskPackages)
            {
                package.ReDefineProjTaskID(Id);
            }
            foreach (var task in Tasks)
            {
                task.ReDefineProjTaskID(Id);
            }
        }

        internal override void InitializeAnalysis(int period = 0, string projectsForecastMethod = "", decimal projectsFACValue = 0M)
        {
            foreach (var package in TaskPackages)
            {
                package.InitializeAnalysis(period, projectsForecastMethod);
            }

            foreach (var task in Tasks)
            {
                task.InitializeAnalysis(period, projectsForecastMethod);
            }

            _analysis = new List<Analysis>();

            if (period > 0)
            {
                foreach (var plan in PlannedProgress)
                {
                    if (plan.Period <= period)
                    {
                        _analysis.Add(new TaskPackageAnalysis(plan.Period, this));
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
                    _analysis.Add(new TaskPackageAnalysis(plan.Period, this));
                }
            }
        }

        internal override void InitAnalysisPreBaseline(List<PreBaselineTaskBase> combinedPreBaselineArr)
        {
            // Iterates through all the task packages, and recurses all the way down to the leaf task package
            foreach (var package in TaskPackages)
            {
                package.InitAnalysisPreBaseline(combinedPreBaselineArr);
            }

            // Iterates through all the task, and recurses all the way down to the leaf task
            foreach (var task in Tasks)
            {
                task.InitAnalysisPreBaseline(combinedPreBaselineArr);
            }

            // Extracts the prebaseline information for a task package based on it's Id, from a list of Depth First Traversed prebaseline informations
            var preBaselineInfo = combinedPreBaselineArr.Find(p => p.SourceId == this.Id);

            if (preBaselineInfo != null)
            {
                // To have data for all the existing planned period and progress of task packages which has already been baselined, it replaces those active calculation with the Historical Data 
                foreach (var plan in PlannedProgress)
                {
                    var historicalPreBases = preBaselineInfo.HistoricalBaselineData;
                    var historicalPreBase = historicalPreBases.Find(hP => hP.Period == plan.Period);

                    // Checks if the Historical Data exists for a specific period, if it does, then it gets substituted by the return of "TaskPackageAnalysisPreBase"
                    if (historicalPreBase != null)
                    {
                        var index = this.Analysis.FindIndex(a => a.Period == plan.Period);
                        Analysis[index] = new TaskPackageAnalysisPreBase(historicalPreBase);
                    }
                }
            }
        }

        public PreBaselineTaskPackage StoreTPAnalysis(int period)
        {
            PreBaselineTaskPackage preBaselineTP = new PreBaselineTaskPackage
            {
                Code = this.Code,
                Id = this.Id,
                PreBaselinePeriod = period,
                SourceId = this.Id,
                Parent = this.Parent,
                Name = this.Name,
                PlannedStartDate = this.PlannedStartDate,
                PlannedEndDate = this.PlannedEndDate,
                PlannedCost = this.PlannedCost ?? 0,
                PlannedDuration = this.PlannedDuration
            };


            for (var i = 0; i < Analysis.Count(); i++)
            {
                HistoricalBaselineData baselineData = new HistoricalBaselineData();
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

                    preBaselineTP.HistoricalBaselineData.Add(baselineData);
                }
            }

            foreach (var package in TaskPackages)
            {
                preBaselineTP.TaskPackages.Add(package.StoreTPAnalysis(period));
            }

            foreach (var task in Tasks)
            {
                preBaselineTP.Tasks.Add(task.StoreTAnalysis(period));
            }

            return preBaselineTP;
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

        public List<TaskBase> DepthFirstTraversal()
        {

            var stack = new List<TaskBase>();
            stack.Add(this as TaskBase);

            this.TaskPackages.ForEach(tp =>
            {
                stack = stack.Concat(tp.DepthFirstTraversal()).ToList();
            });

            stack = stack.Concat(this.Tasks).ToList();

            return stack;
        }

        public string GetDurationWeightAllocation(TaskPackage rootTaskPackage)
        {
            if (this.DurationWeightAllocation != "inherited")
            {
                return this.DurationWeightAllocation;
            }

            var parent = (TaskPackage)rootTaskPackage.DepthFirstTraversal().Find(x => x.Id == this.Parent);
            return parent.GetDurationWeightAllocation(rootTaskPackage);
        }
        public string GetCostWeightAllocation(TaskPackage rootTaskPackage)
        {
            if (this.CostWeightAllocation != "inherited")
            {
                return this.CostWeightAllocation;
            }
            var parent = (TaskPackage)rootTaskPackage.DepthFirstTraversal().Find(x => x.Id == this.Parent);
            return parent.GetCostWeightAllocation(rootTaskPackage);
        }

        private static XmlElement createRootAttributes(XmlElement element, string type, TaskBase taskBase)
        {
            XmlElement task = element.OwnerDocument.CreateElement("Child");
            task.SetAttribute("AssemblyQualifiedName", "Simphony.Project." + type + ", Simphony.Project, Version=4.6.0.0, Culture=neutral, PublicKeyToken=null");
            task.SetAttribute("LocalVersion", "2");
            task.SetAttribute("QuantityValue", "0");
            task.SetAttribute("UnitCostValue", taskBase.PlannedCost.HasValue ? taskBase.PlannedCost.ToString() : "0");
            task.SetAttribute("DurationValue", string.IsNullOrWhiteSpace(taskBase.PlannedDuration) ? "0" : taskBase.PlannedDuration.ToString());
            task.SetAttribute("CostPerDayValue", "0");
            task.SetAttribute("Calendar", "");
            task.SetAttribute("ConstraintDate", "");
            task.SetAttribute("ConstraintType", "AsSoonAsPossible");
            task.SetAttribute("EscalationCategory", "");
            task.SetAttribute("UID", "0");
            task.SetAttribute("ApplyMarkup", "true");
            task.SetAttribute("NonCPM", "false");
            task.SetAttribute("QuantityRange", "");
            task.SetAttribute("UnitCostRange", "");
            task.SetAttribute("DurationRange", "");
            task.SetAttribute("CostPerDayRange", "");
            task.SetAttribute("ForecastMethod", taskBase.ForecastMethod);
            task.SetAttribute("Code", taskBase.Code);
            task.SetAttribute("ID", taskBase.Id);
            task.SetAttribute("Name", taskBase.Name);
            return task;
        }
        private static XmlElement createShape(XmlElement element, string name)
        {
            XmlElement shape = element.OwnerDocument.CreateElement(name);
            shape.SetAttribute("Type", "inherited");
            shape.SetAttribute("IconName", "Blank");
            shape.SetAttribute("Name", "Inherited");
            shape.SetAttribute("Alpha", "0");
            shape.SetAttribute("Beta", "0");
            shape.SetAttribute("Kappa", "0");
            shape.SetAttribute("StringValue", "");
            return shape;
        }
        private static XmlElement createValue(XmlElement element, string name)
        {
            XmlElement el = element.OwnerDocument.CreateElement(name);
            el.SetAttribute("AssemblyQualifiedName", "Simphony.Modeling.DistributionFormula`1[[Simphony.Project.Task, Simphony.Project, Version=4.6.0.0, Culture=neutral, PublicKeyToken=null]], Simphony.Modeling, Version=4.6.0.0, Culture=neutral, PublicKeyToken=0870fe8ecaba8728");
            el.SetAttribute("Version", "325");
            el.SetAttribute("CodeLanguage", "VB");

            XmlElement references = element.OwnerDocument.CreateElement("References");
            XmlElement source = element.OwnerDocument.CreateElement("Source");
            XmlElement value = element.OwnerDocument.CreateElement("Value");
            value.SetAttribute("AssemblyQualifiedName", "Simphony.Mathematics.Constant, Simphony, Version=4.6.0.0, Culture=neutral, PublicKeyToken=0870fe8ecaba8728");
            value.SetAttribute("Value", "0");

            el.AppendChild(references);
            el.AppendChild(source);
            el.AppendChild(value);
            return el;
        }

        private static XmlElement createTask(XmlElement element, TaskBase EVATask, string projectVersion)
        {
            XmlElement task = createRootAttributes(element, "Task", EVATask);
            XmlElement quantityShape = TaskPackage.createShape(element, "QuantityShape");
            XmlElement unitCostShape = TaskPackage.createShape(element, "UnitCostShape");
            XmlElement durationShape = TaskPackage.createShape(element, "DurationShape");
            XmlElement duration = TaskPackage.createValue(element, "Duration");
            XmlElement quantity = TaskPackage.createValue(element, "Quantity");
            XmlElement unitCost = TaskPackage.createValue(element, "UnitCost");
            XmlElement notes = element.OwnerDocument.CreateElement("Notes");
            XmlElement tags = element.OwnerDocument.CreateElement("Tags");

            XmlElement taskValues = element.OwnerDocument.CreateElement("TaskValues");

            foreach (var plannedProgress in EVATask.PlannedProgress)
            {
                taskValues.AppendChild(createPlannedProgressElement(element, plannedProgress));
            }

            taskValues.SetAttribute("CostMean", "0");
            taskValues.SetAttribute("DurationMean", "0");
            taskValues.SetAttribute("GoalCostMean", "0");
            taskValues.SetAttribute("StartDate", "0001-01-01T00:00:00");
            taskValues.SetAttribute("EndDate", "0001-01-01T00:00:00");

            if (Int32.Parse(projectVersion) >= 7)
            {
                // Add costPerDayShape if project version is greater than 7, otherwise let upgrader do it
                XmlElement costPerDayShape = TaskPackage.createShape(element, "CostPerDayShape");
                task.AppendChild(costPerDayShape);
            }

            task.AppendChild(taskValues);
            task.AppendChild(quantityShape);
            task.AppendChild(unitCostShape);
            task.AppendChild(durationShape);
            task.AppendChild(notes);
            task.AppendChild(tags);
            task.AppendChild(duration);
            task.AppendChild(quantity);
            task.AppendChild(unitCost);

            if (Int32.Parse(projectVersion) >= 7)
            {
                // Add costPerDayShape if project version is greater than 7, otherwise let upgrader do it
                XmlElement costPerDay = TaskPackage.createValue(element, "CostPerDay");
                task.AppendChild(costPerDay);
            }

            return task;
        }

        private static XmlElement createPlannedProgressElement(XmlElement element, PlannedProgressEntry plannedProgress)
        {
            XmlElement plannedProgressElement = element.OwnerDocument.CreateElement("PlannedProgress");
            plannedProgressElement.SetAttribute("Period", plannedProgress.Period.ToString());
            plannedProgressElement.SetAttribute("ExpectedProgress", plannedProgress.ExpectedProgress.ToString());
            plannedProgressElement.SetAttribute("ExpectedCost", plannedProgress.ExpectedCost.ToString());
            plannedProgressElement.SetAttribute("PeriodWeight", plannedProgress.PeriodWeight.ToString());
            plannedProgressElement.SetAttribute("NonWorkingPeriod", plannedProgress.NonWorkingPeriod.ToString().ToLower());

            return plannedProgressElement;
        }

        private static XmlElement createTaskPackage(XmlElement element, TaskBase EVATaskPackage, string projectVersion)
        {
            XmlElement task = createRootAttributes(element, "SummaryTask", EVATaskPackage);
            XmlElement quantityShape = TaskPackage.createShape(element, "QuantityShape");
            XmlElement unitCostShape = TaskPackage.createShape(element, "UnitCostShape");
            XmlElement durationShape = TaskPackage.createShape(element, "DurationShape");
            XmlElement notes = element.OwnerDocument.CreateElement("Notes");
            XmlElement tags = element.OwnerDocument.CreateElement("Tags");
            XmlElement children = element.OwnerDocument.CreateElement("Children");

            XmlElement taskValues = element.OwnerDocument.CreateElement("TaskValues");

            foreach (var plannedProgress in EVATaskPackage.PlannedProgress)
            {
                taskValues.AppendChild(createPlannedProgressElement(element, plannedProgress));
            }

            taskValues.SetAttribute("CostMean", "0");
            taskValues.SetAttribute("DurationMean", "0");
            taskValues.SetAttribute("GoalCostMean", "0");
            taskValues.SetAttribute("StartDate", "0001-01-01T00:00:00");
            taskValues.SetAttribute("EndDate", "0001-01-01T00:00:00");


            if (Int32.Parse(projectVersion) >= 7)
            {
                // If project version is greater than 7, add costPerDayShape, otherwise let upgrader do it
                XmlElement costPerDayShape = TaskPackage.createShape(element, "CostPerDayShape");
                task.AppendChild(costPerDayShape);
            }

            task.AppendChild(taskValues);
            task.AppendChild(quantityShape);
            task.AppendChild(unitCostShape);
            task.AppendChild(durationShape);
            task.AppendChild(notes);
            task.AppendChild(tags);
            task.AppendChild(children);

            return task;
        }
        public void ConvertToXML(XmlElement taskNode, string projectVersion)
        {
            if (this == null)
            {
                return;
            }
            else if (this.TaskPackages.Count() == 0 && this.Tasks.Count() == 0)
            {
                return;
            }

            XmlElement taskNodeChildren = (XmlElement)taskNode.SelectSingleNode("Children");

            foreach (var taskPackage in this.TaskPackages)
            {
                XmlElement taskPackageXml = createTaskPackage(taskNodeChildren, taskPackage, projectVersion);
                taskNodeChildren.AppendChild(taskPackageXml);
                taskPackage.ConvertToXML(taskPackageXml, projectVersion);
            }

            foreach (var task in this.Tasks)
            {
                XmlElement taskXml = createTask(taskNodeChildren, task, projectVersion);
                taskNodeChildren.AppendChild(taskXml);
            }
        }
    }
}
