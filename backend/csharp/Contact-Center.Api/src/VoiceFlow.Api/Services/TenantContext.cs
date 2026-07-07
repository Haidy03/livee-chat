using VoiceFlow.Application.Common;

namespace VoiceFlow.Api.Services;

public sealed class TenantContext : ITenantContext
{
    private string? _tenantId;

    public string TenantId => _tenantId ?? throw new InvalidOperationException("Tenant ID has not been resolved.");
    public bool IsResolved => _tenantId is not null;

    public void SetTenantId(string tenantId) => _tenantId = tenantId;
}
