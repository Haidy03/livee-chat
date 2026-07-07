using VoiceFlow.Contracts.Common;

namespace VoiceFlow.Contracts.Visitors;

public sealed class VisitorsQuery : PaginationRequest
{
    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Direction { get; set; }
    public string? Channel { get; set; }
    public string? FinalState { get; set; }
}
