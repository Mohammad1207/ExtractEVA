using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;

namespace EVA_Extract_Actuals
{

    public abstract class PreBaselineTaskBase
    {
        [BsonIgnore]
        public virtual string Id { get; set; }
        public String Code { get; set; }
        public int PreBaselinePeriod { get; set; }
        public string SourceId { get; set; }
        public string Parent { get; set; }
        public string Name { get; set; }
        public virtual bool IsPackage { get; }
        public string PlannedStartDate { get; set; }
        public string PlannedEndDate { get; set; }
        public decimal PlannedCost { get; set; }
        public string PlannedDuration { get; set; }

        public virtual List<HistoricalBaselineData> HistoricalBaselineData { get; set; } = new List<HistoricalBaselineData>();

        internal abstract void ReDefinePreBaselineTaskID(List<TaskBase> dftProj);
    }
}
