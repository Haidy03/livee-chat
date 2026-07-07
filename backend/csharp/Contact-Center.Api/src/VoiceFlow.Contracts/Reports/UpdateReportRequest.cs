
namespace VoiceFlow.Contracts.Reports
{
    public sealed class UpdateReportRequest
    {
        public BiDto? Name { get; set; }
        public BiDto? Description { get; set; }
        public string? Category { get; set; }
        public string? Type { get; set; }
        public string? OwnerId { get; set; }
        public string? Status { get; set; }
        public ReportDefinitionDto? Definition { get; set; }
        public ScheduleDto? Schedule { get; set; }
        public bool? Starred { get; set; }
    }
}
