using Amazon.Runtime.Internal.Transform;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;

namespace EVA_Extract_Actuals
{
    public class BuildDB
    {
        public static EVAProject GenerateProject(XmlDocument sourceFile, XmlNode rootChild, XmlNodeList children)
        {
            var project = new EVAProject();
            var projectInfo = sourceFile.SelectNodes("/ProjectDocument/Project");
            project.Name = projectInfo[0].Attributes["Name"].Value;
            project.FolderName = "Unknown";
            project.OwnerName = "Unknown";
            project.LastSaved = DateOnly.FromDateTime(DateTime.Now);
            project.RootTaskPackage = new TaskPackage();

            List<DurationDates> durationDates = new List<DurationDates>();
            durationDates = PeriodBuilder.GetStartAndEndDateList(durationDates, children);
            var projStartDate = durationDates.Select(dates => dates.StartDate).Min();
            var projEndDate = durationDates.Select(dates => dates.EndDate).Max();

            var periodMap = PeriodBuilder.GetPeriodMap(projStartDate, projEndDate);
            int dispIndex = 0;
            project.RootTaskPackage = GenerateRootTaskPkg(dispIndex, project.RootTaskPackage, rootChild, periodMap);

            return project;
        }

        public static TaskPackage GenerateRootTaskPkg(int dispIndex, TaskPackage rootTaskPkg, XmlNode rootChild, Dictionary<DateOnly, int> periodMap)
        {
            rootTaskPkg.Id = ObjectId.GenerateNewId().ToString();
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
            foreach (XmlElement child in children)
            {
                dispIndex++;
                var objectTypeString = child.GetAttribute("AssemblyQualifiedName");

                if (objectTypeString.Contains("WorkPackage"))
                {
                    TaskPackage taskPkg = new TaskPackage();
                    taskPkg.Id = ObjectId.GenerateNewId().ToString();
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

                    var task = GenerateTask(dispIndex, parentPkg, child, periodMap);
                    parentPkg.Tasks.Add(task);
                    parentPkg.TaskIds.Add(task.Id);
                }
            }

            return parentPkg;
        }

        public static Task GenerateTask(int dispIndex, TaskPackage parentPkg, XmlElement child, Dictionary<DateOnly, int> periodMap)
        {
            var duration = child.GetAttribute("PlannedDuration");
            //var plannedStartDate = DateTime.Parse(child.GetAttribute("PlannedStartDate")).ToUniversalTime();
            var plannedStartDateTime = DateTime.Parse(child.GetAttribute("PlannedStartDate")).ToUniversalTime();
            var plannedStartDate = DateOnly.FromDateTime(plannedStartDateTime);

            var endDateStr = Utils.GetEndDate(plannedStartDate.ToString(), duration);
            //var plannedEndDate = DateTime.Parse(endDateStr).ToUniversalTime();
            var plannedEndDateTime = DateTime.Parse(endDateStr).ToUniversalTime();
            var plannedEndDate = DateOnly.FromDateTime(plannedEndDateTime);
            
            var forecastMethodNode = child.SelectSingleNode("ForecastMethod");

            Task task = new Task();
            task.Id = ObjectId.GenerateNewId().ToString();
            task.DisplayIndex = dispIndex.ToString();
            task.Parent = parentPkg.Id;
            task.ParentDisplayIndex = parentPkg.DisplayIndex;
            task.Name = child.GetAttribute("Name");
            task.ForecastMethod = forecastMethodNode.Attributes["ForecastSelection"].Value;
            task.ConstantForcastValue = forecastMethodNode.Attributes["Constant"].Value.ToString();
            task.PlannedCost = decimal.Parse(child.GetAttribute("PlannedCost"));
            task.PlannedDuration = duration;
            task.PlannedStartDate = plannedStartDate.ToString();
            task.PlannedEndDate = plannedEndDate.ToString();
            if(child.GetAttribute("Distribution") == "UserDefined") { task.Distribution.Code = "Custom"; }
            else if (child.GetAttribute("Distribution") == "FrontLoaded") { task.Distribution.Code = "Beta Front Loaded"; }
            else if (child.GetAttribute("Distribution") == "CenterLoaded") { task.Distribution.Code = "Beta Center Loaded"; }
            else if (child.GetAttribute("Distribution") == "BackLoaded") { task.Distribution.Code = "Beta Back Loaded"; }
            else { task.Distribution.Code = "Uniform"; }

            DateOnly startMonthDate = new(plannedStartDate.Year, plannedStartDate.Month, 1);
            DateOnly endMonthDate = new(plannedEndDate.Year, plannedEndDate.Month, 1);
            int plannedStartPeriod = periodMap[startMonthDate];
            int plannedEndPeriod = periodMap[endMonthDate];

            var actualProgs = child.SelectNodes("Actuals/Actual");
            var trueActualProg = MapTrueActualProg(actualProgs);
            Dictionary<int, decimal> customDistr = new Dictionary<int, decimal>();

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
            int actualStartPeriod = actualPeriodList.Min();
            int actualEndPeriod = actualPeriodList.Max();

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
                else { item.ExpectedProgress = prevProg; }
            }

            task.PlannedProgress = plannedProgress;
            return task;
        }

        public static Dictionary<int, decimal> MapCustomDist(XmlNodeList customWeights)
        {
            decimal plannedProg = 0;
            SortedDictionary<int, decimal> customWeightVal = new SortedDictionary<int, decimal>();
            Dictionary<int, decimal> customDist = new Dictionary<int, decimal>();
            foreach(XmlElement customWeight in customWeights)
            {
                customWeightVal.Add(int.Parse(customWeight.GetAttribute("key")), decimal.Parse(customWeight.ChildNodes.Item(0).Value));
            }

            foreach(var element in customWeightVal)
            {
                plannedProg += element.Value;
                customDist.Add(element.Key, plannedProg);
            }

            return customDist;
        }

        public static Dictionary<int, TrueActualProgressEntry> MapTrueActualProg(XmlNodeList actualProgs)
        {
            Dictionary<int, TrueActualProgressEntry> validActualProg = new Dictionary<int, TrueActualProgressEntry>();

            bool progress = true;
            int index = 0;

            while (progress == true && index < actualProgs.Count-1)
            {
                var currActualProg = (XmlElement)actualProgs.Item(index);
                var currProgress = decimal.Parse(currActualProg.GetAttribute("PercentComplete"));
                var currCost = decimal.Parse(currActualProg.GetAttribute("ActualCost"));

                var nextActualProg = (XmlElement)actualProgs.Item(index + 1);
                var nextProgress = decimal.Parse(nextActualProg.GetAttribute("PercentComplete"));
                var nextCost = decimal.Parse(nextActualProg.GetAttribute("ActualCost"));

                var finalActualProg = (XmlElement)actualProgs.Item(actualProgs.Count - 1);
                var finalProgress = decimal.Parse(finalActualProg.GetAttribute("PercentComplete"));
                var finalCost = decimal.Parse(finalActualProg.GetAttribute("ActualCost"));

                TrueActualProgressEntry trueActualProgressEntry = new TrueActualProgressEntry();
                trueActualProgressEntry.Progress = currProgress;
                trueActualProgressEntry.ActualCost = currCost;
                trueActualProgressEntry.Note = currActualProg.GetAttribute("Note");

                validActualProg.Add(int.Parse(currActualProg.GetAttribute("Period")), trueActualProgressEntry);

                if (nextProgress-currProgress == 0 && nextCost-currCost == 0 && finalProgress-currProgress == 0 && finalCost-currCost == 0) { progress = false; }
                
                index++ ;
            }

            if (progress == true && actualProgs.Count != 0)
            {
                var currActualProg = (XmlElement)actualProgs.Item(index);
                var currProgress = decimal.Parse(currActualProg.GetAttribute("PercentComplete"));
                var currCost = decimal.Parse(currActualProg.GetAttribute("ActualCost"));

                if (actualProgs.Count > 1) {
                    var prevActualProg = (XmlElement)actualProgs.Item(index-1);
                    var prevProgress = decimal.Parse(prevActualProg.GetAttribute("PercentComplete"));
                    var prevCost = decimal.Parse(prevActualProg.GetAttribute("ActualCost"));

                    if (currProgress-prevProgress != 0 || currCost-prevCost != 0) {
                        TrueActualProgressEntry trueActualProgressEntry = new TrueActualProgressEntry();
                        trueActualProgressEntry.Progress = currProgress;
                        trueActualProgressEntry.ActualCost = currCost;
                        trueActualProgressEntry.Note = currActualProg.GetAttribute("Note");
                        validActualProg.Add(int.Parse(currActualProg.GetAttribute("Period")), trueActualProgressEntry); 
                    }
                }

                else
                {
                    if (currProgress != 0 || currCost != 0)
                    {
                        TrueActualProgressEntry trueActualProgressEntry = new TrueActualProgressEntry();
                        trueActualProgressEntry.Progress = currProgress;
                        trueActualProgressEntry.ActualCost = currCost;
                        trueActualProgressEntry.Note = currActualProg.GetAttribute("Note");
                        validActualProg.Add(int.Parse(currActualProg.GetAttribute("Period")), trueActualProgressEntry);
                    }
                }
            }

            return validActualProg;
        }
    }
}
