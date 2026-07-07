namespace VoiceFlow.Core.Interfaces.Services;

public interface IRefreshTokenStore
{
    Task SaveAsync(string userId, string tenantId, string token, DateTime expiresAt, CancellationToken ct = default);
    Task<(string UserId, string TenantId, bool IsValid)> ValidateAsync(string token, CancellationToken ct = default);
    Task RevokeAsync(string token, CancellationToken ct = default);
    Task RevokeAllForUserAsync(string userId, string tenantId, CancellationToken ct = default);
}
