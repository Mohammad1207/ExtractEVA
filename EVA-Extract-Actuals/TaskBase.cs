using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson.Serialization.Attributes;

namespace EVA_Extract_Actuals
{
    public abstract class TaskBase
    {
        protected List<Analysis> _analysis;
        private readonly Dictionary<int, PlannedProgressEntry> _plansToPeriod = new Dictionary<int, PlannedProgressEntry>();
        private readonly Dictionary<int, ActualProgressEntry> _actualsToPeriod = new Dictionary<int, ActualProgressEntry>();

        [BsonIgnore]
        public string ComputedCode { get; set; }

        public String Code { get; set; }
        public string Id { get; set; }
        public string DisplayIndex { get; set; }
        public string Parent { get; set; }
        public string ParentDisplayIndex { get; set; }
        public string Name { get; set; }
        public virtual bool IsPackage { get; }
        public string ForecastMethod { get; set; }
        public string ConstantForcastValue { get; set; }
        public virtual decimal? PlannedCost { get; set; }

        public virtual string PlannedDuration { get; set; }

        public virtual string PlannedStartDate { get; set; }

        public virtual string PlannedEndDate { get; set; }

        public virtual List<ActualProgressEntry> ActualProgress { get; set; } = new List<ActualProgressEntry>();

        public virtual List<PlannedProgressEntry> PlannedProgress { get; set; } = new List<PlannedProgressEntry>();
        public List<Baseline> Baselines { get; set; } = new List<Baseline>();

        public string BuildMD5()
        {
            var hashString = "";
            var dataString = Code + Id + ForecastMethod + Parent + Name + PlannedCost?.ToString() ?? "" + PlannedDuration + PlannedStartDate + PlannedEndDate;
            foreach (var aProgress in ActualProgress)
            {
                dataString += aProgress.Period.ToString() ?? "" + aProgress.Progress.ToString() ?? "" + aProgress.ActualCost.ToString() ?? "" + aProgress.AnticipatedCost.ToString() ?? "" + aProgress.Note;
            }
            foreach (var pProgress in PlannedProgress)
            {
                dataString += pProgress.Period.ToString() ?? "" + pProgress.ExpectedProgress.ToString() ?? "" + pProgress.ExpectedCost.ToString() ?? "" + pProgress.PeriodWeight.ToString() ?? "" + pProgress.NonWorkingPeriod.ToString() ?? "";
            }
            byte[] md5 = MD5.Create().ComputeHash(UTF8Encoding.ASCII.GetBytes(dataString));

            foreach (var md in md5)
            {
                hashString += md.ToString("X2");
            }
            return hashString;
        }

        public virtual List<Analysis> Analysis
        {
            get
            {
                if (_analysis == null)
                {
                    return new List<Analysis>();
                }
                else
                {
                    return _analysis;
                }
            }
        }

        internal virtual int? PlannedStartPeriod
        {
            get
            {
                if (PlannedProgress == null || PlannedProgress.Count == 0)
                {
                    return null;
                }
                return PlannedProgress.Select(entry => entry.Period).Min();
            }
        }

        internal virtual int? PlannedEndPeriod
        {
            get
            {
                if (PlannedProgress == null || PlannedProgress.Count == 0)
                {
                    return null;
                }
                return PlannedProgress.Select(entry => entry.Period).Max();
            }
        }

        internal virtual int? MaxReportedPeriod
        {
            get
            {
                if (ActualProgress == null || ActualProgress.Count == 0)
                {
                    return 0;
                }
                return ActualProgress.Select(entry => entry.Period).Max();
            }
        }

        internal virtual int? MinReportedPeriod
        {
            get
            {
                if (ActualProgress == null || ActualProgress.Count == 0)
                {
                    return 0;
                }
                return ActualProgress.Select(entry => entry.Period).Min();
            }
        }

        internal Analysis FindAnalysis(int period, bool historic)
        {
            var result = _analysis.Where(entry => entry.Period == period).FirstOrDefault();
            if (result == null)
            {
                return new ZeroAnalysis(period);
            }
            return result;
        }

        internal PlannedProgressEntry FindPeriodPlan(int period)
        {
            var result = PlannedProgress.FirstOrDefault(entry => entry.Period == period) ?? new PlannedProgressEntry();
            return result;
        }

        internal PlannedProgressEntry FindPlan(int period)
        {
            if (!_plansToPeriod.TryGetValue(period, out PlannedProgressEntry result))
            {
                var plansToPeriod = PlannedProgress.Where(entry => entry.Period <= period);
                result = new PlannedProgressEntry()
                {
                    Period = period,
                    ExpectedCost = plansToPeriod.Select(entry => entry.ExpectedCost).Sum(),
                    ExpectedProgress = plansToPeriod.Select(entry => entry.ExpectedProgress).Sum(),
                };
                _plansToPeriod[period] = result;
            }
            return result;
        }

        internal ActualProgressEntry FindPeriodActual(int period)
        {
            var result = ActualProgress.FirstOrDefault(entry => entry.Period == period) ?? new ActualProgressEntry();
            return result;
        }

        internal ActualProgressEntry FindActual(int period)
        {
            if (!_actualsToPeriod.TryGetValue(period, out ActualProgressEntry result))
            {
                var actualsToPeriod = ActualProgress.Where(entry => entry.Period <= period);
                var anticipatedActuals = actualsToPeriod.Where(entry => entry.ActualCost == 0 && entry.AnticipatedCost > 0);
                result = new ActualProgressEntry()
                {
                    Period = period,
                    Progress = actualsToPeriod.Select(entry => entry.Progress).Sum(),
                    ActualCost = actualsToPeriod.Select(entry => entry.ActualCost).Sum(),
                    AnticipatedCost = anticipatedActuals.Select(entry => entry.AnticipatedCost).Sum(),
                };
                _actualsToPeriod[period] = result;
            }
            return result;
        }

        internal List<HistoricalBaselineData> GetHistoricalBaselineData()
        {
            return this.Baselines.Select(baseline => baseline.HistoricalBaselineData).SelectMany(x => x).ToList();
        }
    }
}
