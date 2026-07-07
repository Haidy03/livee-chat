using Microsoft.AspNetCore.SignalR;
using VoiceFlow.Api.LiveChat.Application;
using VoiceFlow.Api.LiveChat.Application.Dtos;

namespace VoiceFlow.Api.LiveChat.Hubs;

public class CustomerHub : Hub
{
    private readonly ClientRequestService _requests;
    private readonly RoomService _room;

    public CustomerHub(ClientRequestService requests, RoomService room)
    {
        _requests = requests;
        _room = room;
    }

    public async Task StartChat(StartChatPayload payload)
    {
        payload.ConnectionId = Context.ConnectionId;
        var id = await _requests.CreateRequestAsync(payload);
        await Clients.Caller.SendAsync("RequestQueued", new { requestId = id });
    }

    public Task SendMessage(string roomId, string text) =>
        _room.HandleInboundMessageAsync(roomId, text);

    public Task Typing(string roomId, bool isTyping) =>
        Clients.OthersInGroup($"room:{roomId}").SendAsync("CustomerTyping", new { roomId, isTyping });

    public Task GoOffline(string? requestId) =>
        string.IsNullOrEmpty(requestId) ? Task.CompletedTask : _requests.MarkOfflineAsync(requestId);
}
