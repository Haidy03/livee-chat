

namespace VoiceFlow.Contracts.Reports
{
    public sealed class ReportResultColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string DataType { get; set; } = "string";
    }
    public sealed class ReportResultResponse
    {
        public string Id { get; set; } = string.Empty;
        public string ReportId { get; set; } = string.Empty;
        public string RunId { get; set; } = string.Empty;
        public string GeneratedAt { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public List<ReportResultColumnDto> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public Dictionary<string, object?> Summary { get; set; } = new();
    }
}
