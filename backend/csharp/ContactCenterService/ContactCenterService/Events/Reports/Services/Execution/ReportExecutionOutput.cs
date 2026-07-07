using VoiceFlow.Core.Entities.Reports;

namespace Contact_Center.Worker.Events.Reports.Services.Execution;

/// <summary>Shape returned by every report builder: columns, rows and a free-form summary.</summary>
public sealed record ReportExecutionOutput(
    IReadOnlyList<ReportResultColumn> Columns,
    IReadOnlyList<Dictionary<string, object?>> Rows,
    IReadOnlyDictionary<string, object?> Summary);
