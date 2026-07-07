using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Contracts.Rbac;

public sealed class CreateRoleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, List<string>> Permissions { get; set; } = [];
}
