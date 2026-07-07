using System.Security.Claims;
using VoiceFlow.Api.Services;
using VoiceFlow.Application.Common;

namespace VoiceFlow.Api.Middleware;

public sealed class TenantContextMiddleware
{
    private readonly RequestDelegate _next;

    public TenantContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantId = context.User.FindFirstValue("tenant_id");
            if (!string.IsNullOrWhiteSpace(tenantId) && tenantContext is TenantContext resolved)
            {
                resolved.SetTenantId(tenantId);
            }
        }

        await _next(context);
    }
}
