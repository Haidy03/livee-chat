namespace VoiceFlow.Contracts.Rbac;

public sealed class UserRoleResponse
{
    public string UserId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
}
