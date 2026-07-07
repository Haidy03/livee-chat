using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Calls;

/// <summary>
/// Upserts a call row keyed by SIP Call-ID (stored on Call.CallId).
/// </summary>
public sealed class SoftphoneCallUpsertRequest
{
    [Required]
    public string SipCallId { get; set; } = string.Empty;

    /// <summary>Frontend values: "in" | "out".</summary>
    public string? Direction { get; set; }

    /// <summary>Frontend softphone status text e.g. ringing, completed.</summary>
    public string? Status { get; set; }

    public string? Caller { get; set; }
    public string? Called { get; set; }

    public string? CallerId { get; set; }
    public string? CallerName { get; set; }
    public string? CallerExtension { get; set; }
    public bool? CallerIsAiAgent { get; set; }
    public string? CalledId { get; set; }
    public string? CalledName { get; set; }
    public string? CalledExtension { get; set; }
    public bool? CalledIsAiAgent { get; set; }




    public string? FromUri { get; set; }
    public string? FromDisplay { get; set; }
    public string? ToUri { get; set; }
    public string? ToDisplay { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? HangupCause { get; set; }
    public string? RecordingUrl { get; set; }
    public bool? HasRecording { get; set; }
    public int? RingSeconds { get; set; }
    public int? HoldSeconds { get; set; }
    public int? ActiveSeconds { get; set; }
    public int? TotalHoldSeconds { get; set; }
    public int? TotalSeconds { get; set; }
}
