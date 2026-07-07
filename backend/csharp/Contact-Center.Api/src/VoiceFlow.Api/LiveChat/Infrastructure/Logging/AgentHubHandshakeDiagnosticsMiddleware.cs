using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Logging;

public sealed class AgentHubHandshakeDiagnosticsMiddleware
{
    private const string Category = "AgentHubHandshake";
    private readonly RequestDelegate _next;
    private readonly ILogger<AgentHubHandshakeDiagnosticsMiddleware> _logger;

    public AgentHubHandshakeDiagnosticsMiddleware(
        RequestDelegate next,
        ILogger<AgentHubHandshakeDiagnosticsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AgentHubLogWriter writer)
    {
        if (!context.Request.Path.StartsWithSegments("/hubs/agent"))
        {
            await _next(context);
            return;
        }

        var startedAt = DateTime.UtcNow;
        var baseProperties = BuildProperties(context, startedAt);

        await writer.TryWriteAsync(
            "Information",
            Category,
            "AgentHub request reached backend",
            baseProperties);

        context.Response.OnStarting(async state =>
        {
            var (httpContext, logWriter, requestStartedAt) = ((HttpContext, AgentHubLogWriter, DateTime))state;
            var props = BuildProperties(httpContext, requestStartedAt);
            props["statusCode"] = httpContext.Response.StatusCode;
            props["elapsedMs"] = (long)(DateTime.UtcNow - requestStartedAt).TotalMilliseconds;

            await logWriter.TryWriteAsync(
                "Information",
                Category,
                "AgentHub response starting",
                props);
        }, (context, writer, startedAt));

        try
        {
            await _next(context);

            var props = BuildProperties(context, startedAt);
            props["statusCode"] = context.Response.StatusCode;
            props["elapsedMs"] = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;

            await writer.TryWriteAsync(
                "Information",
                Category,
                "AgentHub request pipeline completed",
                props);
        }
        catch (Exception ex)
        {
            var props = BuildProperties(context, startedAt);
            props["elapsedMs"] = (long)(DateTime.UtcNow - startedAt).TotalMilliseconds;
            props["exceptionType"] = ex.GetType().FullName;
            props["exceptionMessage"] = ex.Message;

            await writer.TryWriteAsync(
                "Error",
                Category,
                $"AgentHub pipeline exception: {ex.GetType().FullName}: {ex.Message}",
                props,
                ex);

            _logger.LogError(ex, "AgentHub pipeline exception trace={TraceIdentifier}", context.TraceIdentifier);
            throw;
        }
    }

    private static Dictionary<string, object?> BuildProperties(HttpContext context, DateTime startedAt) => new()
    {
        ["traceIdentifier"] = context.TraceIdentifier,
        ["connectionId"] = context.Connection.Id,
        ["startedAt"] = startedAt,
        ["method"] = context.Request.Method,
        ["path"] = context.Request.Path.Value,
        ["isWebSocketRequest"] = context.WebSockets.IsWebSocketRequest,
        ["hasAccessTokenQuery"] = context.Request.Query.ContainsKey("access_token"),
        ["hasAuthorizationHeader"] = context.Request.Headers.ContainsKey("Authorization"),
        ["origin"] = context.Request.Headers.Origin.ToString(),
        ["isAuthenticated"] = context.User.Identity?.IsAuthenticated,
        ["userIdentityName"] = context.User.Identity?.Name,
        ["sub"] = context.User.FindFirstValue("sub"),
        ["nameIdentifier"] = context.User.FindFirstValue(ClaimTypes.NameIdentifier),
        ["tenantId"] = context.User.FindFirstValue("tenant_id"),
    };
}