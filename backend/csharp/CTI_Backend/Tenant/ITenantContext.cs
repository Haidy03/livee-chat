namespace CtiBackend.Tenant;

/// <summary>
/// Per-request tenant identity. Today this is populated by
/// <see cref="TenantContextMiddleware"/> from trusted X-Tenant-Id /
/// X-User-Id headers (sent by the frontend BFF/proxy behind the
/// X-API-Key gate). Swap this implementation for a JWT-based one
/// when the C# backend takes over auth directly.
/// </summary>
public interface ITenantContext
{
    string? TenantId { get; }
    string? UserId { get; }
    bool IsAuthenticated { get; }
}

internal sealed class TenantContext : ITenantContext
{
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(TenantId) && !string.IsNullOrWhiteSpace(UserId);
}
