using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

namespace EVA_Extract_Actuals
{

    public class Baseline
    {
        public int BaselineNumber { get; set; }
        public string DateEffective { get; set; }
        public string PlannedStartDate { get; set; }
        public string PlannedEndDate { get; set; }
        public decimal PlannedCost { get; set; }
        public string PlannedDuration { get; set; }
        public List<HistoricalBaselineData> HistoricalBaselineData { get; set; }
    }

    public class NBaseline
    {
        public int BaselineNumber { get; set; }
        public string DateEffective { get; set; }
        public int PreBaselinePeriod { get; set; }
        public string UnqPreBaslineId { get; set; }     // A unique Id generated based on the OwnerName, Name, LastSaved, and baselineNumber;
    }

    public class HistoricalBaselineData
    {
        public int Period { get; set; }
        public decimal PeriodWeight { get; set; }
        public decimal HistoricalActualCost { get; set; }
        public decimal HistoricalActualCost_P { get; set; }
        public decimal HistoricalActualProgress { get; set; }
        public decimal HistoricalBAC { get; set; }
        public decimal HistoricalBSpent { get; set; }
        public decimal HistoricalCPI { get; set; }
        public decimal HistoricalCPI_P { get; set; }
        public decimal HistoricalCostVariance { get; set; }
        public decimal HistoricalEarnedSchedule { get; set; }
        public decimal HistoricalEarnedSchedule_P { get; set; }
        public decimal HistoricalEarnedValue { get; set; }
        public decimal HistoricalEarnedValue_P { get; set; }
        public decimal HistoricalBudgetedRateForecast { get; set; }
        public decimal HistoricalPastCostPerformanceForecast { get; set; }
        public decimal HistoricalPastSchedulePerformanceForecast { get; set; }
        public decimal HistoricalScheduleAndCostIndexedForecast { get; set; }
        public decimal HistoricalFAC_Mean { get; set; }
        public decimal HistoricalFAC_SD { get; set; }
        public decimal HistoricalPlannedProgress { get; set; }
        public decimal HistoricalPlannedCost { get; set; }
        public decimal HistoricalPlannedCost_P { get; set; }
        public decimal HistoricalSPI_C { get; set; }
        public decimal HistoricalSPI_CP { get; set; }
        public decimal HistoricalSPI_T { get; set; }
        public decimal HistoricalSPI_TP { get; set; }
        public decimal HistoricalScheduleVariance_C { get; set; }
        public decimal HistoricalScheduleVariance_T { get; set; }
        public decimal HistoricalVAC { get; set; }
        public string HistoricalNote { get; set; }
    }
}