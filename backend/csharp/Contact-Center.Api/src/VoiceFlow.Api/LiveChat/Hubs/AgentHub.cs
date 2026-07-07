using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using VoiceFlow.Api.LiveChat.Application;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;
using VoiceFlow.Api.LiveChat.Infrastructure.Logging;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Api.LiveChat.Hubs;

[Authorize]
public class AgentHub : Hub
{
    private readonly IProfileRepository _profiles;
    private readonly IRoomRepository _rooms;
    private readonly IPresenceStore _presence;
    private readonly RoutingEngine _routing;
    private readonly RoomService _roomService;
    private readonly ILogger<AgentHub> _logger;
    private readonly AgentHubLogWriter _hubLogWriter;

    public AgentHub(
        IProfileRepository profiles,
        IRoomRepository rooms,
        IPresenceStore presence,
        RoutingEngine routing,
        RoomService roomService,
        ILogger<AgentHub> logger,
        AgentHubLogWriter hubLogWriter)
    {
        _profiles = profiles;
        _rooms = rooms;
        _presence = presence;
        _routing = routing;
        _roomService = roomService;
        _logger = logger;
        _hubLogWriter = hubLogWriter;
    }


    private string? TryGetAgentId() =>
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User?.FindFirstValue("sub");

    private string AgentId => TryGetAgentId() ?? throw new HubException("no_agent_identity");

    private string? TenantId => Context.User?.FindFirstValue("tenant_id");

    private Dictionary<string, object?> BuildConnectionProperties(string? agentId = null, string? tenantId = null) => new()
    {
        ["connectionId"] = Context.ConnectionId,
        ["agentId"] = agentId ?? TryGetAgentId(),
        ["tenantId"] = tenantId ?? TenantId,
        ["sub"] = Context.User?.FindFirstValue("sub"),
        ["nameIdentifier"] = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier),
        ["isAuthenticated"] = Context.User?.Identity?.IsAuthenticated,
    };

    private Task WriteConnectionLogAsync(
        string level,
        string message,
        IDictionary<string, object?>? properties = null,
        Exception? exception = null)
    {
        var props = BuildConnectionProperties();
        if (properties is not null)
        {
            foreach (var (key, value) in properties)
                props[key] = value;
        }

        return _hubLogWriter.TryWriteAsync(level, nameof(AgentHub), message, props, exception);
    }

    private async Task RejectAsync(string reason, IDictionary<string, object?>? properties = null, Exception? exception = null)
    {
        var props = properties is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(properties);
        props["reason"] = reason;

        await WriteConnectionLogAsync(
            exception is null ? "Warning" : "Error",
            $"AgentHub connection rejected: {reason}",
            props,
            exception);

        try
        {
            await Clients.Caller.SendAsync("ConnectionRejected", new { reason });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AgentHub failed to notify caller of rejection reason={Reason}", reason);
        }
        Context.Abort();
    }

    public override async Task OnConnectedAsync()
    {
        var agentId = TryGetAgentId();
        var tenantId = TenantId;

        await WriteConnectionLogAsync("Information", "AgentHub.OnConnectedAsync entered", new Dictionary<string, object?>
        {
            ["agentId"] = agentId,
            ["tenantId"] = tenantId,
        });

        _logger.LogInformation(
            "AgentHub connect claims sub={Sub} nameid={NameId} tenant={Tenant} conn={ConnectionId}",
            Context.User?.FindFirstValue("sub"),
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier),
            tenantId,
            Context.ConnectionId);

        if (string.IsNullOrWhiteSpace(agentId))
        {
            _logger.LogWarning("AgentHub connect rejected: no_agent_identity conn={ConnectionId}", Context.ConnectionId);
            await RejectAsync("no_agent_identity");
            return;
        }

        try
        {
            VoiceFlow.Core.Entities.Profile? profile;
            try
            {
                await WriteConnectionLogAsync("Information", "AgentHub profile lookup started", new Dictionary<string, object?>
                {
                    ["agentId"] = agentId,
                    ["tenantId"] = tenantId,
                    ["lookupMode"] = string.IsNullOrWhiteSpace(tenantId) ? "userId" : "userIdAndTenant",
                });

                profile = string.IsNullOrWhiteSpace(tenantId)
                    ? await _profiles.GetByUserIdAsync(agentId)
                    : await _profiles.GetByUserIdAndTenantAsync(agentId, tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "AgentHub connect failed while loading profile agentId={AgentId} tenantId={TenantId} conn={ConnectionId}",
                    agentId, tenantId, Context.ConnectionId);
                await RejectAsync("profile_lookup_failed", new Dictionary<string, object?>
                {
                    ["agentId"] = agentId,
                    ["tenantId"] = tenantId,
                    ["exceptionType"] = ex.GetType().FullName,
                    ["exceptionMessage"] = ex.Message,
                }, ex);
                return;
            }

            if (profile is null)
            {
                _logger.LogWarning(
                    "AgentHub connect rejected: profile_not_found agentId={AgentId} tenantId={TenantId}",
                    agentId, tenantId);
                await RejectAsync("profile_not_found", new Dictionary<string, object?>
                {
                    ["agentId"] = agentId,
                    ["tenantId"] = tenantId,
                });
                return;
            }

            await WriteConnectionLogAsync("Information", "AgentHub profile lookup succeeded", new Dictionary<string, object?>
            {
                ["profileId"] = profile.Id,
                ["profileUserId"] = profile.UserId,
                ["profileTenantId"] = profile.TenantId,
                ["profileStatus"] = profile.Status,
                ["profileDisabled"] = profile.Disabled,
                ["groupCount"] = profile.Groups?.Count ?? 0,
            });

            // Accept any non-inactive status. Profiles across tenants use
            // varied labels (active/online/available/away/busy). Only reject
            // when explicitly disabled or the status marks the account as
            // suspended/inactive/offline.
            var normalizedStatus = (profile.Status ?? string.Empty).Trim().ToLowerInvariant();
            var rejectedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "inactive", "suspended", "disabled", "banned", "deleted", "offline",
            };
            if (profile.Disabled || rejectedStatuses.Contains(normalizedStatus))
            {
                _logger.LogWarning(
                    "AgentHub connect rejected: profile_inactive agentId={AgentId} disabled={Disabled} status={Status}",
                    agentId, profile.Disabled, profile.Status);
                await RejectAsync("profile_inactive", new Dictionary<string, object?>
                {
                    ["agentId"] = agentId,
                    ["tenantId"] = tenantId,
                    ["profileStatus"] = profile.Status,
                    ["profileDisabled"] = profile.Disabled,
                });
                return;
            }

            var agent = new Agent
            {
                Id = agentId,
                DisplayName = string.IsNullOrWhiteSpace(profile.FullName) ? agentId : profile.FullName,
                DepartmentIds = profile.Groups?
                    .Where(group => !string.IsNullOrWhiteSpace(group))
                    .ToList() ?? new List<string>(),
                Languages = string.IsNullOrWhiteSpace(profile.Language)
                    ? new List<string>()
                    : new List<string> { profile.Language },
                MaxConcurrency = 4,
                VoiceEnabled = false
            };

            try
            {
                await WriteConnectionLogAsync("Information", "AgentHub presence registration started", new Dictionary<string, object?>
                {
                    ["agentId"] = agentId,
                    ["tenantId"] = tenantId,
                    ["departmentIds"] = string.Join(",", agent.DepartmentIds),
                    ["languages"] = string.Join(",", agent.Languages),
                });

                await _presence.HydrateAsync(agent, Context.ConnectionId);
                await Groups.AddToGroupAsync(Context.ConnectionId, $"agent:{agentId}");

                await WriteConnectionLogAsync("Information", "AgentHub presence registration completed", new Dictionary<string, object?>
                {
                    ["agentId"] = agentId,
                    ["tenantId"] = tenantId,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "AgentHub connect failed while registering presence/groups agentId={AgentId} tenantId={TenantId} conn={ConnectionId}",
                    agentId, tenantId, Context.ConnectionId);
                await RejectAsync("presence_unavailable", new Dictionary<string, object?>
                {
                    ["agentId"] = agentId,
                    ["tenantId"] = tenantId,
                    ["exceptionType"] = ex.GetType().FullName,
                    ["exceptionMessage"] = ex.Message,
                }, ex);
                return;
            }

            try
            {
                var activeRooms = await _rooms.GetActiveByAgentAsync(agentId);
                await WriteConnectionLogAsync("Information", "AgentHub active rooms loaded", new Dictionary<string, object?>
                {
                    ["agentId"] = agentId,
                    ["tenantId"] = tenantId,
                    ["activeRoomCount"] = activeRooms.Count,
                });

                foreach (var room in activeRooms)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"room:{room._id}");

                await Clients.Caller.SendAsync("ActiveRooms", activeRooms);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "AgentHub connect failed while loading active rooms agentId={AgentId} tenantId={TenantId} conn={ConnectionId}",
                    agentId, tenantId, Context.ConnectionId);
                await RejectAsync("livechat_database_unavailable", new Dictionary<string, object?>
                {
                    ["agentId"] = agentId,
                    ["tenantId"] = tenantId,
                    ["exceptionType"] = ex.GetType().FullName,
                    ["exceptionMessage"] = ex.Message,
                }, ex);
                return;
            }

            _logger.LogInformation(
                "AgentHub connected agentId={AgentId} tenantId={TenantId} depts={Depts} conn={ConnectionId}",
                agentId, tenantId, string.Join(",", agent.DepartmentIds), Context.ConnectionId);

            await WriteConnectionLogAsync("Information", "AgentHub connected", new Dictionary<string, object?>
            {
                ["agentId"] = agentId,
                ["tenantId"] = tenantId,
                ["departmentIds"] = string.Join(",", agent.DepartmentIds),
            });

            await base.OnConnectedAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AgentHub connect failed for agentId={AgentId} tenantId={TenantId} conn={ConnectionId}",
                agentId, tenantId, Context.ConnectionId);
            await RejectAsync("connect_failed", new Dictionary<string, object?>
            {
                ["agentId"] = agentId,
                ["tenantId"] = tenantId,
                ["exceptionType"] = ex.GetType().FullName,
                ["exceptionMessage"] = ex.Message,
            }, ex);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var agentId = TryGetAgentId();
        await WriteConnectionLogAsync(exception is null ? "Information" : "Warning", "AgentHub.OnDisconnectedAsync entered", new Dictionary<string, object?>
        {
            ["agentId"] = agentId,
            ["tenantId"] = TenantId,
            ["exceptionType"] = exception?.GetType().FullName,
            ["exceptionMessage"] = exception?.Message,
        }, exception);

        try
        {
            if (!string.IsNullOrWhiteSpace(agentId))
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                var cleanup = Task.Run(async () =>
                {
                    await _presence.RemoveConnectionAsync(agentId, Context.ConnectionId);
                    if (!await _presence.HasConnectionsAsync(agentId))
                        await _presence.SetStatusAsync(agentId, AgentStatus.Offline);
                }, cts.Token);

                var completed = await Task.WhenAny(cleanup, Task.Delay(Timeout.Infinite, cts.Token));
                if (completed != cleanup)
                    throw new TimeoutException("presence cleanup exceeded 2s");
                await cleanup; // observe exceptions
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "AgentHub disconnect cleanup failed agentId={AgentId} conn={ConnectionId}",
                agentId, Context.ConnectionId);

            await WriteConnectionLogAsync("Warning", "AgentHub disconnect cleanup failed", new Dictionary<string, object?>
            {
                ["agentId"] = agentId,
                ["tenantId"] = TenantId,
                ["exceptionType"] = ex.GetType().FullName,
                ["exceptionMessage"] = ex.Message,
            }, ex);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task SetStatus(string status)
    {
        if (!Enum.TryParse<AgentStatus>(status, true, out var s))
            throw new HubException("invalid_status");
        await _presence.SetStatusAsync(AgentId, s);
        if (s == AgentStatus.Available)
            await _routing.DispatchAllPendingAsync();
    }

    public async Task AcceptRequest(string requestId)
    {
        var room = await _routing.AcceptAsync(requestId, AgentId, Context.ConnectionId);
        await Clients.Caller.SendAsync("RoomStarted", room);
    }

    public Task DeclineRequest(string requestId) =>
        _routing.ReleaseAndRequeueAsync(requestId, AgentId);

    public Task SendMessage(string roomId, string text) =>
        _roomService.HandleAgentMessageAsync(roomId, AgentId, text);

    public Task Typing(string roomId, bool isTyping) =>
        Clients.OthersInGroup($"room:{roomId}").SendAsync("TypingIndicator", new { roomId, agentId = AgentId, isTyping });

    public Task CloseRoom(string roomId, string typeOfClose) =>
        _roomService.CloseAsync(roomId, AgentId, typeOfClose);

    public Task TransferRoom(string roomId, string targetId)
    {
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(targetId))
            throw new HubException("invalid_transfer_args");
        return _roomService.TransferAsync(roomId, AgentId, targetId);
    }

    public Task SendAttachment(string roomId, MessageAttachment attachment, string? caption)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            throw new HubException("invalid_room");
        if (attachment is null || string.IsNullOrWhiteSpace(attachment.Url))
            throw new HubException("invalid_attachment");
        return _roomService.HandleAgentAttachmentAsync(roomId, AgentId, attachment, caption);
    }
}
