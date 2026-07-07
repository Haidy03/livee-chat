

namespace VoiceFlow.Contracts.Reports
{
    public sealed class CreateReportRequest
    {
        public BiDto Name { get; set; } = new();
        public BiDto Description { get; set; } = new();
        public string Category { get; set; } = "Operations";
        public string Type { get; set; } = "Operational";
        public string OwnerId { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft";
        public ReportDefinitionDto Definition { get; set; } = new();
        public ScheduleDto Schedule { get; set; } = new();
        public bool Starred { get; set; }
    }
}
