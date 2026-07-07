
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Contracts.Events
{
    public sealed class ReportRunRequested
    {
        public string RunId { get; set; } = string.Empty;
        public string ReportId { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public string TriggeredBy { get; set; } = string.Empty;
        public ReportRunTrigger Trigger { get; set; } = ReportRunTrigger.Manual;
        public DateTimeOffset RequestedAt { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string Event { get; set; } = "ReportRunRequested";
        /// <summary>Optional 1-based page used by Detail-mode executions.</summary>
        public int Page { get; set; } = 1;
        /// <summary>Optional page size used by Detail-mode executions.</summary>
        public int PageSize { get; set; } = 50;
    }

    public static class ReportMessagingConstants
    {
        public const string Exchange = "reports";
        public const string DeadLetterExchange = "reports.dlx";
        public const string RunRequestedRoutingKey = "report.run.requested";
        public const string RunRequestedQueue = "reports.run.requested";
        public const string RunRequestedDlq = "reports.run.requested.dlq";
    }
}
