using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Auth;

public sealed class SignupRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string OrgName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
