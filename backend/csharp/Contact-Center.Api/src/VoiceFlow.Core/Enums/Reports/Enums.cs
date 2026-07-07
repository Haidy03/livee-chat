namespace VoiceFlow.Core.Enums.Reports;

public enum ReportCategory { Operations, Workforce, Quality, CustomerExperience, Executive }
public enum ReportType { Operational, Analytical, ExecutiveSummary }
public enum ReportStatus { Active, Paused, Draft }
public enum VizId { Kpi, Line, Bar, Pie, Area, Table, Heatmap, Funnel, Gauge, Sankey }
public enum ScheduleFrequency { Once, Hourly, Daily, Weekly, Monthly, Cron }
public enum ExportFormat { Pdf, Xlsx, Csv, Html }
public enum ReportRunStatus { Queued, Running, Succeeded, Failed }
public enum ReportRunTrigger { Manual, Scheduled }

/// <summary>How the report is executed against the underlying data source.</summary>
public enum ReportMode
{
    /// <summary>One row per source document; user picks columns.</summary>
    Detail = 1,
    /// <summary>Grouped aggregation with dimensions + metrics.</summary>
    MetricAndDimension = 2,
}

public enum SortDirection { Asc, Desc }
