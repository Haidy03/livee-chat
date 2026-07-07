using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Logging;

/// <summary>
/// SignalR hub filter that wraps hub construction, connection, disconnection and every
/// method invocation. Any exception thrown inside the pipeline (including DI resolution
/// failures during hub activation) is persisted to the AgentHubLog Mongo collection
/// BEFORE SignalR closes the socket with code 1011, so the real cause is never lost.
/// </summary>
public sealed class AgentHubDiagnosticsFilter : IHubFilter
{
    private readonly IServiceProvider _root;
    private readonly ILogger<AgentHubDiagnosticsFilter> _logger;

    public AgentHubDiagnosticsFilter(IServiceProvider root, ILogger<AgentHubDiagnosticsFilter> logger)
    {
        _root = root;
        _logger = logger;
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        var hubName = context.Hub.GetType().Name;
        if (!string.Equals(hubName, "AgentHub", StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        var connId = context.Context.ConnectionId;
        var sub = context.Context.User?.FindFirstValue("sub");
        var nameId = context.Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var tenant = context.Context.User?.FindFirstValue("tenant_id");

        await WriteAsync("Information", $"{hubName}.OnConnectedAsync entered", new()
        {
            ["hub"] = hubName,
            ["connectionId"] = connId,
            ["sub"] = sub,
            ["nameid"] = nameId,
            ["tenant_id"] = tenant,
            ["userIdentityName"] = context.Context.User?.Identity?.Name,
            ["isAuthenticated"] = context.Context.User?.Identity?.IsAuthenticated,
        });

        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await WriteAsync("Error", $"{hubName}.OnConnectedAsync threw {ex.GetType().FullName}: {ex.Message}", new()
            {
                ["hub"] = hubName,
                ["connectionId"] = connId,
                ["exceptionType"] = ex.GetType().FullName,
                ["exceptionMessage"] = ex.Message,
                ["stackTrace"] = ex.ToString(),
            });
            throw;
        }
    }

    public async Task OnDisconnectedAsync(HubLifetimeContext context, Exception? exception, Func<HubLifetimeContext, Exception?, Task> next)
    {
        var hubName = context.Hub.GetType().Name;
        if (!string.Equals(hubName, "AgentHub", StringComparison.Ordinal))
        {
            await next(context, exception);
            return;
        }

        await WriteAsync(exception is null ? "Information" : "Warning",
            $"{hubName}.OnDisconnectedAsync exception={exception?.GetType().FullName}",
            new()
            {
                ["hub"] = hubName,
                ["connectionId"] = context.Context.ConnectionId,
                ["exceptionType"] = exception?.GetType().FullName,
                ["exceptionMessage"] = exception?.Message,
                ["stackTrace"] = exception?.ToString(),
            });

        try
        {
            await next(context, exception);
        }
        catch (Exception ex)
        {
            await WriteAsync("Error", $"{hubName}.OnDisconnectedAsync inner threw {ex.GetType().FullName}", new()
            {
                ["exceptionType"] = ex.GetType().FullName,
                ["stackTrace"] = ex.ToString(),
            });
            throw;
        }
    }

    public async ValueTask<object?> InvokeMethodAsync(HubInvocationContext invocationContext, Func<HubInvocationContext, ValueTask<object?>> next)
    {
        var hubName = invocationContext.Hub.GetType().Name;
        if (!string.Equals(hubName, "AgentHub", StringComparison.Ordinal))
            return await next(invocationContext);

        var method = invocationContext.HubMethodName;
        try
        {
            return await next(invocationContext);
        }
        catch (Exception ex)
        {
            await WriteAsync("Error", $"{hubName}.{method} threw {ex.GetType().FullName}: {ex.Message}", new()
            {
                ["hub"] = hubName,
                ["method"] = method,
                ["connectionId"] = invocationContext.Context.ConnectionId,
                ["exceptionType"] = ex.GetType().FullName,
                ["exceptionMessage"] = ex.Message,
                ["stackTrace"] = ex.ToString(),
            });
            throw;
        }
    }

    private async Task WriteAsync(string level, string message, Dictionary<string, object?> properties)
    {
        var entry = new AgentHubLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Category = "AgentHubDiagnosticsFilter",
            Message = message,
            Properties = properties.ToDictionary(kv => kv.Key, kv => kv.Value?.ToString()),
        };

        try
        {
            using var scope = _root.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LiveChatMongoContext>();
            await ctx.AgentHubLogs.InsertOneAsync(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AgentHubDiagnosticsFilter failed to persist log: {Message}", message);
        }
    }
}
