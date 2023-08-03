using Amazon.Runtime.Internal.Transform;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;

namespace EVA_Extract_Actuals
{
    public class BuildDB
    {
        public static EVAProject GenerateProject(string projectName, XmlDocument sourceFile)
        {
            var project = new EVAProject();
            var projectRoot = sourceFile.SelectNodes("/ProjectDocument/Project");
            var children = sourceFile.SelectNodes("/ProjectDocument/Project/Children/Child");
            project.Name = projectName;
            project.FolderName = "Unknown";
            project.OwnerName = "Unknown";
            project.LastSaved = DateTime.Now;
            project.RootTaskPackage = new TaskPackage();

            List<DurationDates> durationDates = new List<DurationDates>();
            durationDates = PeriodBuilder.GetStartAndEndDateList(durationDates, children);
            var projStartDate = durationDates.Select(dates => dates.StartDate).Min();
            var projEndDate = durationDates.Select(dates => dates.EndDate).Max();

            var periodMap = PeriodBuilder.GetPeriodMap(projStartDate, projEndDate);
            int dispIndex = 0;
            project.RootTaskPackage = GenerateRootTaskPkg(dispIndex, project.RootTaskPackage, projectRoot[0], periodMap);

            return project;
        }

        public static TaskPackage GenerateRootTaskPkg(int dispIndex, TaskPackage rootTaskPkg, XmlNode rootChild, Dictionary<DateOnly, int> periodMap)
        {
            rootTaskPkg.Id = ObjectId.GenerateNewId().ToString();
            rootTaskPkg.Code = rootChild.Attributes["CodeOverride"].Value;
            rootTaskPkg.Name = rootChild.Attributes["Name"].Value;
            rootTaskPkg.DisplayIndex = dispIndex.ToString();
            rootTaskPkg.Parent = "";
            rootTaskPkg.ParentDisplayIndex = "";

            XmlNodeList children = rootChild.SelectNodes("Children/Child");
            rootTaskPkg = GenerateTaskAndPkg(dispIndex, rootTaskPkg, children, periodMap);

            return rootTaskPkg;
        }

        public static TaskPackage GenerateTaskAndPkg(int dispIndex, TaskPackage parentPkg, XmlNodeList children, Dictionary<DateOnly, int> periodMap)
        {
            int index = 1;
            foreach (XmlElement child in children)
            {
                dispIndex++;
                var objectTypeString = child.GetAttribute("AssemblyQualifiedName");

                if (objectTypeString.Contains("WorkPackage"))
                {
                    TaskPackage taskPkg = new TaskPackage();
                    taskPkg.Id = ObjectId.GenerateNewId().ToString();
                    taskPkg.Code = child.GetAttribute("CodeOverride") != string.Empty ? child.GetAttribute("CodeOverride") : string.Format(CultureInfo.CurrentCulture, "{0}.{1}", parentPkg.Code, index);
                    taskPkg.DisplayIndex = dispIndex.ToString();
                    taskPkg.Parent = parentPkg.Id;
                    taskPkg.ParentDisplayIndex = parentPkg.DisplayIndex;
                    taskPkg.Name = child.GetAttribute("Name");
                    taskPkg.CostWeightAllocation = (1).ToString();
                    taskPkg.DurationWeightAllocation = (0).ToString();

                    
                    XmlNodeList taskPkgChildren = child.SelectNodes("Children/Child");
                    taskPkg = GenerateTaskAndPkg(dispIndex, taskPkg, taskPkgChildren, periodMap);
                    parentPkg.TaskPackages.Add(taskPkg);
                }
                else if (objectTypeString.Contains("WorkTask"))
                {

                    var task = GenerateTask(index, dispIndex, parentPkg, child, periodMap);
                    parentPkg.Tasks.Add(task);
                    parentPkg.TaskIds.Add(task.Id);
                }
                index++;
            }

            return parentPkg;
        }

        public static Task GenerateTask(int index, int dispIndex, TaskPackage parentPkg, XmlElement child, Dictionary<DateOnly, int> periodMap)
        {
            var duration = child.GetAttribute("PlannedDuration");
            var plannedStartDateTime = DateTime.Parse(child.GetAttribute("PlannedStartDate")).ToUniversalTime();
            var plannedStartDate = DateOnly.FromDateTime(plannedStartDateTime);

            var endDateStr = Utils.GetEndDate(plannedStartDate.ToString(), duration);
            var plannedEndDateTime = DateTime.Parse(endDateStr).ToUniversalTime();
            var plannedEndDate = DateOnly.FromDateTime(plannedEndDateTime);
            
            var forecastMethodNode = child.SelectSingleNode("ForecastMethod");

            Task task = new()
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Code = child.GetAttribute("CodeOverride") != string.Empty ? child.GetAttribute("CodeOverride") : string.Format(CultureInfo.CurrentCulture, "{0}.{1}", parentPkg.Code, index),
                DisplayIndex = dispIndex.ToString(),
                Parent = parentPkg.Id,
                ParentDisplayIndex = parentPkg.DisplayIndex,
                Name = child.GetAttribute("Name"),
                ForecastMethod = forecastMethodNode.Attributes["ForecastSelection"].Value,
                ConstantForcastValue = forecastMethodNode.Attributes["Constant"].Value.ToString(),
                PlannedCost = decimal.Parse(child.GetAttribute("PlannedCost")),
                PlannedDuration = duration,
                PlannedStartDate = plannedStartDate.ToString(),
                PlannedEndDate = plannedEndDate.ToString()
            };
            if (child.GetAttribute("Distribution") == "UserDefined") { task.Distribution.Code = "Custom"; }
            else if (child.GetAttribute("Distribution") == "FrontLoaded") { task.Distribution.Code = "BetaFrontLoaded"; }
            else if (child.GetAttribute("Distribution") == "CenterLoaded") { task.Distribution.Code = "BetaCenterLoaded"; }
            else if (child.GetAttribute("Distribution") == "BackLoaded") { task.Distribution.Code = "BetaBackLoaded"; }
            else { task.Distribution.Code = "Uniform"; }

            DateOnly startMonthDate = new(plannedStartDate.Year, plannedStartDate.Month, 1);
            DateOnly endMonthDate = new(plannedEndDate.Year, plannedEndDate.Month, 1);
            int plannedStartPeriod = periodMap[startMonthDate];
            int plannedEndPeriod = periodMap[endMonthDate];

            var actualProgs = child.SelectNodes("Actuals/Actual");
            var trueActualProg = MapTrueActualProg(actualProgs);
            Dictionary<int, decimal> customDistr = new();

            task = FillActualProgress(task, startMonthDate, endMonthDate, plannedStartPeriod, plannedEndPeriod, trueActualProg);

            if (child.GetAttribute("Distribution") == "UserDefined") { 
                customDistr = MapCustomDist(child.SelectNodes("CustomWeights/CustomWeight"));
                task = FillPlannedProgress(task, customDistr);
            }
            
            task = PlanBuilder.CalculatePlan(task);

            var database = Database.GetDatabase();
            var taskCollection = database.GetCollection<Task>("TASKS");
            taskCollection.InsertOne(task);

            return task;
        }
 
        public static Task FillActualProgress (Task task, DateOnly startMonthDate, DateOnly endMonthDate, int plannedStartPeriod, int plannedEndPeriod, Dictionary<int, TrueActualProgressEntry> trueActualProg)
        {
            List<int> actualPeriodList = new List<int>(trueActualProg.Keys);
            int actualStartPeriod = actualPeriodList == null ? actualPeriodList.Min():0;
            int actualEndPeriod = actualPeriodList == null ? actualPeriodList.Max():0;

            int startPeriod = Math.Min(plannedStartPeriod, actualStartPeriod);
            int endPeriod = Math.Max(plannedEndPeriod, actualEndPeriod);

            for (int i=startPeriod; i<=endPeriod; i++)
            {
                var taskActual = new ActualProgressEntry();
                taskActual.Period = i;

                if (trueActualProg.ContainsKey(i))
                {
                    var trueActualProgressEntry = trueActualProg[i];
                    taskActual.Progress = trueActualProgressEntry.Progress;
                    taskActual.ActualCost = trueActualProgressEntry.ActualCost;
                    taskActual.Note = trueActualProgressEntry.Note;
                }
                else
                {
                    taskActual.Progress = 0;
                    taskActual.ActualCost = 0;
                    taskActual.Note = "";
                }

                var taskPlan = new PlannedProgressEntry();
                taskPlan.Period = i;

                decimal periodWeight;
                if (i >= plannedStartPeriod && i <= plannedEndPeriod)
                {
                    periodWeight = PeriodBuilder.GetPeriodWeight(i, plannedStartPeriod, plannedEndPeriod, startMonthDate, endMonthDate, task);
                }
                else { periodWeight = 0; }

                var nonWorkingPeriod = periodWeight < 0.0001M;
                taskPlan.PeriodWeight = periodWeight;
                taskPlan.NonWorkingPeriod = nonWorkingPeriod;

                task.ActualProgress.Add(taskActual);
                task.PlannedProgress.Add(taskPlan);
            }

            return task;
        }

        public static Task FillPlannedProgress( Task task, Dictionary<int, decimal> customDistr)
        {
            var plannedProgress = task.PlannedProgress;
            decimal prevProg = 0;

            foreach (var item in plannedProgress)
            {
                var period = item.Period;
                if (customDistr.ContainsKey(period)) { 
                    item.ExpectedProgress = customDistr[period];
                    prevProg = customDistr[period];
                }
                else { item.ExpectedProgress = 0; }
            }

            task.PlannedProgress = plannedProgress;
            return task;
        }

        public static Dictionary<int, decimal> MapCustomDist(XmlNodeList customWeights)
        {
            Dictionary<int, decimal> customDist = new Dictionary<int, decimal>();
            foreach(XmlElement customWeight in customWeights)
            {
                customDist.Add(int.Parse(customWeight.GetAttribute("key")), decimal.Parse(customWeight.ChildNodes.Item(0).Value));
            }

            return customDist;
        }

        public static Dictionary<int, TrueActualProgressEntry> MapTrueActualProg(XmlNodeList actualProgs)
        {
            Dictionary<int, TrueActualProgressEntry> validActualProg = new Dictionary<int, TrueActualProgressEntry>();

            bool progress = true;
            int index = 0;

            if (actualProgs.Count > 0)
            {
                var firstActualProg = (XmlElement)actualProgs.Item(index);
                var firstProgress = decimal.Parse(firstActualProg.GetAttribute("PercentComplete"));
                var firstCost = decimal.Parse(firstActualProg.GetAttribute("ActualCost"));

                if (actualProgs.Count == 1 && firstProgress == 0 && firstCost == 0) { progress = false; }
                else
                {
                    TrueActualProgressEntry trueActualProgressEntry = new TrueActualProgressEntry();
                    trueActualProgressEntry.Progress = firstProgress;
                    trueActualProgressEntry.ActualCost = firstCost;
                    trueActualProgressEntry.Note = firstActualProg.GetAttribute("Note");

                    validActualProg.Add(int.Parse(firstActualProg.GetAttribute("Period")), trueActualProgressEntry);
                }
                index++;
            }
            

            while (progress == true && index < actualProgs.Count)
            {
                var prevActualProg = (XmlElement)actualProgs.Item(index - 1);
                var prevProgress = decimal.Parse(prevActualProg.GetAttribute("PercentComplete"));
                var prevCost = decimal.Parse(prevActualProg.GetAttribute("ActualCost"));
                
                var currActualProg = (XmlElement)actualProgs.Item(index);
                var currProgress = decimal.Parse(currActualProg.GetAttribute("PercentComplete"));
                var currCost = decimal.Parse(currActualProg.GetAttribute("ActualCost"));
                
                var finalActualProg = (XmlElement)actualProgs.Item(actualProgs.Count - 1);
                var finalProgress = decimal.Parse(finalActualProg.GetAttribute("PercentComplete"));
                var finalCost = decimal.Parse(finalActualProg.GetAttribute("ActualCost"));

                if (Math.Abs(prevProgress - currProgress) == 0 && Math.Abs(prevCost - currCost) == 0 && Math.Abs(finalProgress - currProgress) == 0 && Math.Abs(finalCost - currCost) == 0) { progress = false; }
                else 
                {
                    TrueActualProgressEntry trueActualProgressEntry = new TrueActualProgressEntry();
                    trueActualProgressEntry.Progress = currProgress - prevProgress;
                    trueActualProgressEntry.ActualCost = currCost - prevCost;
                    trueActualProgressEntry.Note = currActualProg.GetAttribute("Note");

                    validActualProg.Add(int.Parse(currActualProg.GetAttribute("Period")), trueActualProgressEntry);
                }
                index++ ;
            }

            return validActualProg;
        }
    }
}
