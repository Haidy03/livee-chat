using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CtiBackend.Tenant;

public interface ITenantAdminPolicy
{
    ValueTask<bool> IsTenantAdminAsync(string tenantId, string userId, CancellationToken ct);
}

/// <summary>
/// TODO: wire this to the real role provider once roles are exposed
/// to CtiBackend. Today it accepts any authenticated tenant context.
/// </summary>
public sealed class DefaultTenantAdminPolicy : ITenantAdminPolicy
{
    public ValueTask<bool> IsTenantAdminAsync(string tenantId, string userId, CancellationToken ct)
        => ValueTask.FromResult(true);
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireTenantAdminAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
    {
        var tenant = ctx.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
        if (!tenant.IsAuthenticated)
        {
            ctx.Result = new UnauthorizedObjectResult(new { success = false, errors = new[] { "unauthorized" } });
            return;
        }

        var policy = ctx.HttpContext.RequestServices.GetRequiredService<ITenantAdminPolicy>();
        var ok = await policy.IsTenantAdminAsync(tenant.TenantId!, tenant.UserId!, ctx.HttpContext.RequestAborted);
        if (!ok)
        {
            ctx.Result = new ObjectResult(new { success = false, errors = new[] { "forbidden" } })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
