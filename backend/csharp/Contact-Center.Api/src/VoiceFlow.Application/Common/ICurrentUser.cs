namespace VoiceFlow.Application.Common;

public interface ICurrentUser
{
    string UserId { get; }
    string TenantId { get; }
    string Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}
