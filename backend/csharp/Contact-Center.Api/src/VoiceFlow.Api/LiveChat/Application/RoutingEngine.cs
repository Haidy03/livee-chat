using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;
using VoiceFlow.Api.LiveChat.Hubs;
using VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

namespace VoiceFlow.Api.LiveChat.Application;

public sealed class RoutingEngine
{
    private readonly IClientRequestRepository _requests;
    private readonly IRoomRepository _rooms;
    private readonly IPresenceStore _presence;
    private readonly IOfferTimeoutStore _timeouts;
    private readonly IHubContext<AgentHub> _agentHub;
    private readonly LiveChatMongoContext _mongo;
    private readonly ILogger<RoutingEngine> _log;

    private static readonly TimeSpan OfferTtl = TimeSpan.FromSeconds(20);

    public RoutingEngine(
        IClientRequestRepository requests,
        IRoomRepository rooms,
        IPresenceStore presence,
        IOfferTimeoutStore timeouts,
        IHubContext<AgentHub> agentHub,
        LiveChatMongoContext mongo,
        ILogger<RoutingEngine> log)
    {
        _requests = requests;
        _rooms = rooms;
        _presence = presence;
        _timeouts = timeouts;
        _agentHub = agentHub;
        _mongo = mongo;
        _log = log;
    }

    public async Task TryDispatchAsync(string requestId, CancellationToken ct = default)
    {
        var req = await _requests.GetAsync(requestId, ct);
        if (req is null || req.locked || req.status?.state == "offline") return;

        var excluded = (req.execludedAgentId ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var agentId = await _presence.SelectAgentAsync(req.department?.id ?? string.Empty, req.lang, excluded);
        if (agentId is null) return;

        var locked = await _requests.TryLockAsync(requestId, ct);
        if (locked is null) return; // lost race

        await _presence.IncrementLoadAsync(agentId);
        await _timeouts.ArmAsync(requestId, agentId, OfferTtl);

        await _agentHub.Clients.Group($"agent:{agentId}").SendAsync("RequestOffered", new
        {
            requestId = locked._id,
            channel = locked.channel,
            department = locked.department,
            lang = locked.lang,
            clientInfo = locked.clientInfo,
            requestCount = locked.requestCount,
        }, ct);
    }

    public async Task DispatchAllPendingAsync(CancellationToken ct = default)
    {
        var pending = await _requests.GetUnlockedPendingAsync(ct);
        foreach (var r in pending)
            await TryDispatchAsync(r._id, ct);
    }

    public async Task OnAgentCapacityFreedAsync(string agentId, CancellationToken ct = default)
    {
        try
        {
            var presence = await _presence.GetAsync(agentId);
            if (presence?.HasCapacity == true)
                await DispatchAllPendingAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "OnAgentCapacityFreedAsync failed for agent {AgentId}", agentId);
        }
    }

    public async Task<Room> AcceptAsync(string requestId, string agentId, string agentConnectionId, CancellationToken ct = default)
    {
        Room? room = null;
        using var session = await _mongo.Client.StartSessionAsync(cancellationToken: ct);

        await session.WithTransactionAsync(async (s, token) =>
        {
            var req = await _requests.GetAsync(requestId, s, token);
            if (req is null || !req.locked)
                throw new HubException("request_gone");

            room = new Room
            {
                agentId = agentId,
                agentConnectionId = agentConnectionId,
                channel = req.channel,
                agentChannel = req.agentChannel,
                projectId = req.projectId,
                chatbotId = req.chatbotId,
                contactId = req.contact_Id,
                clientId = req.userId,
                lang = req.lang,
                clientInfo = req.clientInfo,
                department = req.department,
                clientConnectionId = req.connectionId,
                roomStatus = "active",
                created = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
            };

            await _rooms.InsertAsync(room, s, token);

            var deleted = await _requests.DeleteAsync(req._id, s, token);
            if (deleted != 1) throw new HubException("request_delete_failed");

            return true;
        }, cancellationToken: ct);

        // Post-commit
        await _timeouts.CancelAsync(requestId);
        await _presence.TouchLastAssignedAsync(agentId);
        await _agentHub.Groups.AddToGroupAsync(agentConnectionId, $"room:{room!._id}", ct);
        return room;
    }

    public async Task ReleaseAndRequeueAsync(string requestId, string agentId, CancellationToken ct = default)
    {
        await _presence.DecrementLoadAsync(agentId);
        await _timeouts.CancelAsync(requestId);
        await _requests.UnlockAsync(requestId, agentId, ct);
        await TryDispatchAsync(requestId, ct);
        await OnAgentCapacityFreedAsync(agentId, ct);
    }
}
