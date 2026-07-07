using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
