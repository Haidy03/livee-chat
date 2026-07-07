namespace VoiceFlow.Contracts.Rbac;

public sealed class UpdateRoleRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public Dictionary<string, List<string>>? Permissions { get; set; }
}
