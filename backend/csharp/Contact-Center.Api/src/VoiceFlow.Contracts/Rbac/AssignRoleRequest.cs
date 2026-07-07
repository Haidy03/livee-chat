using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Rbac;

public sealed class AssignRoleRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    [Required]
    public string RoleId { get; set; } = string.Empty;
}
