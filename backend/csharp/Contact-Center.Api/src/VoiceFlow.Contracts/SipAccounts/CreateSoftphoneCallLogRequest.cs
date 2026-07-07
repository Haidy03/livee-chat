using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.SipAccounts;

public sealed class CreateSoftphoneCallLogRequest
{
    [Required]
    public string Direction { get; set; } = string.Empty;
    [Required]
    public string Status { get; set; } = string.Empty;
    [Required]
    public string Number { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ContactId { get; set; }
    [Required]
    public DateTime StartedAt { get; set; }
    public int DurationSec { get; set; }
    public string FailureReason { get; set; } = string.Empty;
}
