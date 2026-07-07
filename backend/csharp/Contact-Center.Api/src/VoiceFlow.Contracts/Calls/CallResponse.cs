using VoiceFlow.Api.Calls;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Calls;

public sealed class CallResponse
{
    public string Id { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string? CallId { get; init; }
    public CallDirection Direction { get; init; }
    public CallStatus Status { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? AnsweredAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int RingSeconds { get; init; }
    public int HoldSeconds { get; init; }
    public int ActiveSeconds { get; init; }
    public int TotalHoldSeconds { get; init; }
    public string? HangupCause { get; init; }
    public string? AgentId { get; init; }
    public string? GroupId { get; init; }
    public string Caller { get; init; } = string.Empty;
    public string Called { get; init; } = string.Empty;

    public string? CallerId { get; set; }
    public string? CallerName { get; set; }
    public string? CallerExtension { get; set; }
    public bool? CallerIsAiAgent { get; set; }
    public string? CalledId { get; set; }
    public string? CalledName { get; set; }
    public string? CalledExtension { get; set; }
    public bool? CalledIsAiAgent { get; set; }


    public string? FromUri { get; init; }
    public string? FromDisplay { get; init; }
    public string? ToUri { get; init; }
    public string? ToDisplay { get; init; }
    public List<string> TagIds { get; init; } = [];
    public List<string> AutoTagIds { get; init; } = [];
    public int TotalSeconds { get; init; }
    public bool HasRecording { get; init; }
    public string? RecordingUrl { get; init; }
    public string Notes { get; init; } = string.Empty;
    public string Inputs { get; init; } = string.Empty;
    public string? Summary { get; init; }
    public string? SummaryLanguage { get; init; }
    public string? SummaryAccuracyFeedback { get; init; }
    public string? FullTranscript { get; init; }
    public List<TranscriptSegment>? Segments { get; set; }
    public Sentiment? Sentiment { get; init; }

    public WrapUpDto? WrapUp { get; init; }
}
