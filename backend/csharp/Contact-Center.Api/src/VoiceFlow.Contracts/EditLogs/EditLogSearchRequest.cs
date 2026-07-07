using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Contracts.EditLogs;

public sealed class EditLogSearchRequest : PaginationRequest
{
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? Action { get; set; }
    public string? UserId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? SummarySearch { get; set; }
}
