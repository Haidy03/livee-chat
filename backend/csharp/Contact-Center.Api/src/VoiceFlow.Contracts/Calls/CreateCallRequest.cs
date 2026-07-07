using System.ComponentModel.DataAnnotations;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Contracts.Calls;

public sealed class CreateCallRequest
{
    [Required]
    public CallDirection Direction { get; set; }
    [Required]
    public CallStatus Status { get; set; }
    [Required]
    public DateTime StartedAt { get; set; }
    public string Caller { get; set; } = string.Empty;
    public string Called { get; set; } = string.Empty;
    public int TotalSeconds { get; set; }
    public string? CallId { get; set; }
}
