using Microsoft.AspNetCore.Http;

namespace CtiBackend.Tenant;

public sealed class TenantContextMiddleware
{
    public const string TenantHeader = "X-Tenant-Id";
    public const string UserHeader = "X-User-Id";

    private readonly RequestDelegate _next;
    public TenantContextMiddleware(RequestDelegate next) => _next = next;

    public Task Invoke(HttpContext ctx, ITenantContext tenant)
    {
        if (tenant is TenantContext concrete)
        {
            if (ctx.Request.Headers.TryGetValue(TenantHeader, out var t) && !string.IsNullOrWhiteSpace(t))
                concrete.TenantId = t.ToString().Trim();
            if (ctx.Request.Headers.TryGetValue(UserHeader, out var u) && !string.IsNullOrWhiteSpace(u))
                concrete.UserId = u.ToString().Trim();
        }
        return _next(ctx);
    }
}
