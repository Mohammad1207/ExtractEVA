using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace EVA_Extract_Actuals
{

    public class PreBaselineTaskPackage : PreBaselineTaskBase
    {

        public override string Id { get; set; }

        [BsonIgnore]
        public override bool IsPackage { get; } = true;

        [BsonIgnore]
        public virtual List<PreBaselineTask> Tasks { get; set; } = new List<PreBaselineTask>();

        public List<string> PrebaselineTaskIds { get; set; } = new List<string>();

        public virtual List<PreBaselineTaskPackage> TaskPackages { get; set; } = new List<PreBaselineTaskPackage>();

        public List<PreBaselineTaskBase> DepthFirstTraversal()
        {

            var stack = new List<PreBaselineTaskBase>();
            stack.Add(this as PreBaselineTaskBase);

            this.TaskPackages.ForEach(tp =>
            {
                stack = stack.Concat(tp.DepthFirstTraversal()).ToList();
            });

            stack = stack.Concat(this.Tasks).ToList();

            return stack;
        }

        public void SaveTasks(IMongoCollection<PreBaselineTask> dbCollection)
        {
            if (this.Tasks.Count > 0)
            {
                dbCollection.InsertMany(this.Tasks);
                foreach (var task in Tasks)
                {
                    PrebaselineTaskIds.Add(task.Id);
                }
            }
            foreach (var package in TaskPackages)
            {
                package.SaveTasks(dbCollection);
            }
        }

        public void SaveAsTasks(IMongoCollection<PreBaselineTask> dbCollection)
        {
            if (this.Tasks.Count > 0)
            {
                this.Tasks.ForEach(task => {
                    var filter = Builders<PreBaselineTask>.Filter.Eq(t => t.Id, task.Id);
                    dbCollection.ReplaceOne(filter, task);
                });
            }
            foreach (var package in TaskPackages)
            {
                package.SaveAsTasks(dbCollection);
            }
        }

        public void HydrateTasks(IMongoCollection<PreBaselineTask> dbCollection)
        {
            var filter = Builders<PreBaselineTask>.Filter.In(p => p.Id, this.PrebaselineTaskIds);
            var tasks = dbCollection.Find(filter).ToList();
            this.Tasks.AddRange(tasks);

            foreach (var package in TaskPackages)
            {
                package.HydrateTasks(dbCollection);
            }
        }

        internal override void ReDefinePreBaselineTaskID(List<TaskBase> dftProj)
        {
            var projTask = dftProj.Find(p => p.Code == this.Code);
            this.SourceId = projTask.Id;
            this.Parent = projTask.Parent;

            foreach (var package in TaskPackages)
            {
                package.ReDefinePreBaselineTaskID(dftProj);
            }
            foreach (var task in Tasks)
            {
                task.ReDefinePreBaselineTaskID(dftProj);
            }
        }

    }

}

