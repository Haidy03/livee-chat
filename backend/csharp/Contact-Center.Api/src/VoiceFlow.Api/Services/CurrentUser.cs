using System.Security.Claims;
using VoiceFlow.Application.Common;

namespace VoiceFlow.Api.Services;

public sealed class CurrentUser : ICurrentUser
{
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        IsAuthenticated = user?.Identity?.IsAuthenticated ?? false;

        if (IsAuthenticated && user is not null)
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            TenantId = user.FindFirstValue("tenant_id") ?? string.Empty;
            Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList().AsReadOnly();
        }
    }

    public string UserId { get; } = string.Empty;
    public string TenantId { get; } = string.Empty;
    public string Email { get; } = string.Empty;
    public IReadOnlyList<string> Roles { get; } = [];
    public bool IsAuthenticated { get; }
}
