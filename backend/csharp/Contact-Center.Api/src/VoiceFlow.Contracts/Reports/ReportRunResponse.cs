namespace VoiceFlow.Contracts.Reports;

public sealed class ReportRunResponse
{
    public string Id { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public string StartedAt { get; set; } = string.Empty;
    public string? FinishedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public long? DurationMs { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public int? RowCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResultId { get; set; }
}
