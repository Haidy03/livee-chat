using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Auth;

/// <summary>Creates an AuthUser + Profile inside an existing tenant (owners/admins only).</summary>
public sealed class InviteUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string Timezone { get; set; } = "UTC+00:00";
    public string Language { get; set; } = "English";
    public bool BrowserNotifications { get; set; }
    /// <summary>owner | admin | agent</summary>
    public string Role { get; set; } = "agent";

    public List<string>? Groups { get; set; }
    public int? ExtensionNumber { get; set; }
}
