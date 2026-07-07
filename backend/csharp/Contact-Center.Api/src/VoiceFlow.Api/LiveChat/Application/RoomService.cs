using Microsoft.AspNetCore.SignalR;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;
using VoiceFlow.Api.LiveChat.Hubs;

namespace VoiceFlow.Api.LiveChat.Application;

public sealed class RoomService
{
    private readonly IRoomRepository _rooms;
    private readonly IPresenceStore _presence;
    private readonly ChannelDispatcher _dispatcher;
    private readonly IHubContext<AgentHub> _agentHub;
    private readonly RoutingEngine _routing;

    public RoomService(
        IRoomRepository rooms,
        IPresenceStore presence,
        ChannelDispatcher dispatcher,
        IHubContext<AgentHub> agentHub,
        RoutingEngine routing)
    {
        _rooms = rooms;
        _presence = presence;
        _dispatcher = dispatcher;
        _agentHub = agentHub;
        _routing = routing;
    }

    public async Task HandleInboundMessageAsync(string roomId, string text, CancellationToken ct = default)
    {
        var room = await _rooms.GetAsync(roomId, ct);
        if (room is null || room.roomStatus != "active") return;

        var msg = new Message
        {
            RoomId = roomId,
            Direction = MessageDirection.Inbound,
            SenderType = SenderType.Customer,
            SenderId = room.contactId,
            Text = text,
            Timestamp = DateTime.UtcNow,
        };
        await _rooms.AppendMessageAsync(roomId, msg, ct);
        await _agentHub.Clients.Group($"room:{roomId}").SendAsync("MessageReceived", msg, ct);
    }

    public async Task HandleAgentMessageAsync(string roomId, string agentId, string text, CancellationToken ct = default)
    {
        var room = await _rooms.GetAsync(roomId, ct);
        if (room is null || room.roomStatus != "active") return;

        var msg = new Message
        {
            RoomId = roomId,
            Direction = MessageDirection.Outbound,
            SenderType = SenderType.Agent,
            SenderId = agentId,
            Text = text,
            Timestamp = DateTime.UtcNow,
        };
        await _rooms.AppendMessageAsync(roomId, msg, ct);
        await _agentHub.Clients.Group($"room:{roomId}").SendAsync("MessageReceived", msg, ct);
        await _dispatcher.SendAsync(room, msg, ct);
    }

    public async Task HandleAgentAttachmentAsync(
        string roomId,
        string agentId,
        MessageAttachment attachment,
        string? caption,
        CancellationToken ct = default)
    {
        var room = await _rooms.GetAsync(roomId, ct);
        if (room is null || room.roomStatus != "active") return;

        var msg = new Message
        {
            RoomId = roomId,
            Direction = MessageDirection.Outbound,
            SenderType = SenderType.Agent,
            SenderId = agentId,
            Text = caption ?? string.Empty,
            Attachments = new List<MessageAttachment> { attachment },
            Timestamp = DateTime.UtcNow,
        };
        await _rooms.AppendMessageAsync(roomId, msg, ct);
        await _agentHub.Clients.Group($"room:{roomId}").SendAsync("MessageReceived", msg, ct);
        await _dispatcher.SendAsync(room, msg, ct);
    }

    public async Task CloseAsync(string roomId, string agentId, string typeOfClose, CancellationToken ct = default)
    {
        await _rooms.CloseAsync(roomId, typeOfClose, ct);
        await _presence.DecrementLoadAsync(agentId);
        await _agentHub.Clients.Group($"room:{roomId}").SendAsync("RoomClosed", new
        {
            roomId,
            typeOfClose,
        }, ct);
        await _routing.OnAgentCapacityFreedAsync(agentId, ct);
    }

    public async Task TransferAsync(string roomId, string fromAgentId, string targetId, CancellationToken ct = default)
    {
        var room = await _rooms.GetAsync(roomId, ct);
        if (room is null || room.roomStatus != "active")
            throw new Microsoft.AspNetCore.SignalR.HubException("room_gone");

        var targetPresence = await _presence.GetAsync(targetId);
        var isAgentTarget = targetPresence is not null;

        if (isAgentTarget)
        {
            await _rooms.ReassignAgentAsync(roomId, targetId, ct);

            var toConnection = await _presence.GetAnyConnectionAsync(targetId);
            if (!string.IsNullOrWhiteSpace(toConnection))
                await _agentHub.Groups.AddToGroupAsync(toConnection, $"room:{roomId}", ct);

            await _presence.DecrementLoadAsync(fromAgentId);
            await _presence.IncrementLoadAsync(targetId);

            await _agentHub.Clients.Group($"room:{roomId}").SendAsync("RoomTransferred", new
            {
                roomId,
                fromAgentId,
                toAgentId = targetId,
            }, ct);
            await _agentHub.Clients.Group($"agent:{targetId}").SendAsync("RoomTransferred", new
            {
                roomId,
                fromAgentId,
                toAgentId = targetId,
            }, ct);

            await _routing.OnAgentCapacityFreedAsync(fromAgentId, ct);
        }
        else
        {
            await _rooms.RequeueToGroupAsync(roomId, targetId, ct);
            await _presence.DecrementLoadAsync(fromAgentId);

            await _agentHub.Clients.Group($"room:{roomId}").SendAsync("RoomTransferred", new
            {
                roomId,
                fromAgentId,
                toGroupId = targetId,
            }, ct);

            await _routing.OnAgentCapacityFreedAsync(fromAgentId, ct);
        }
    }
}
