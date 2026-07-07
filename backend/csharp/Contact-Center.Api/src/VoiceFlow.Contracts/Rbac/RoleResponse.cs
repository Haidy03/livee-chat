namespace VoiceFlow.Contracts.Rbac;

public sealed class RoleResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsSystem { get; init; }
    public Dictionary<string, List<string>> Permissions { get; init; } = [];
}
