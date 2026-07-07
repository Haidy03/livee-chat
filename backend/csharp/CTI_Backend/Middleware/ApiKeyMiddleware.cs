using CtiBackend.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CtiBackend.Middleware;

/// <summary>
/// Simple opt-in API-key gate. When ApiSecurity:Enabled is true, all
/// /api/cti/* requests must carry the configured X-API-Key header.
/// </summary>
public sealed class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApiSecurityOptions _options;

    public ApiKeyMiddleware(RequestDelegate next, IOptions<ApiSecurityOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task Invoke(HttpContext ctx)
    {
        if (!_options.Enabled || !ctx.Request.Path.StartsWithSegments("/api/cti"))
        {
            await _next(ctx);
            return;
        }

        if (!ctx.Request.Headers.TryGetValue(_options.ApiKeyHeaderName, out var supplied) ||
            !string.Equals(supplied.ToString(), _options.ApiKey, StringComparison.Ordinal))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsync("Invalid or missing API key");
            return;
        }

        await _next(ctx);
    }
}
