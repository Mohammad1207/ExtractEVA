using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EVA_Extract_Actuals
{

    public class EVAProject
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public DateTime LastSaved { get; set; }

        public string OwnerName { get; set; }

        public string Name { get; set; }

        public string FolderName { get; set; }

        public string CurrentHash { get; set; }

        public List<NBaseline> Baselines { get; set; } = new List<NBaseline>();

        public TaskPackage RootTaskPackage { get; set; }
        public bool IsArchive { get; set; }

    }

    public class ActualProgressEntry
    {

        public int Period { get; set; }

        public decimal? Progress { get; set; } = 0M;

        public decimal? ActualCost { get; set; } = 0M;

        public decimal? AnticipatedCost { get; set; } = 0M;

        public string Note { get; set; }

    }

    public class PlannedProgressEntry
    {

        public int Period { get; set; }

        public decimal ExpectedProgress { get; set; } = 0M;

        public decimal ExpectedCost { get; set; } = 0M;

        public decimal PeriodWeight { get; set; } = 0M;

        public bool NonWorkingPeriod { get; set; }

    }

    public class DurationDates
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }

    public class TrueActualProgressEntry
    {
        public decimal? Progress { get; set; } = 0M;
        public decimal? ActualCost { get; set; } = 0M;
        public decimal? AnticipatedCost { get; set; } = 0M;
        public string Note { get; set; }
    }
}
