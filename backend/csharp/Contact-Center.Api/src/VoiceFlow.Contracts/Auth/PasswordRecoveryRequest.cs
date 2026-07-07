using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Auth;

public sealed class PasswordRecoveryRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
