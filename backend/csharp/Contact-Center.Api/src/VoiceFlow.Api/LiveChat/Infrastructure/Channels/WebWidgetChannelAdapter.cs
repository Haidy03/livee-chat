using Microsoft.AspNetCore.SignalR;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;
using VoiceFlow.Api.LiveChat.Hubs;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Channels;

public sealed class WebWidgetChannelAdapter : IChannelAdapter
{
    private readonly IHubContext<CustomerHub> _hub;
    public WebWidgetChannelAdapter(IHubContext<CustomerHub> hub) => _hub = hub;
    public string ChannelKey => "webwidget";
    public Task SendToCustomerAsync(Room room, Message message, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(room.clientConnectionId)) return Task.CompletedTask;
        return _hub.Clients.Client(room.clientConnectionId).SendAsync("MessageReceived", message, ct);
    }
}

public sealed class MobileAppChannelAdapter : IChannelAdapter
{
    private readonly IHubContext<CustomerHub> _hub;
    public MobileAppChannelAdapter(IHubContext<CustomerHub> hub) => _hub = hub;
    public string ChannelKey => "mobileapp";
    public Task SendToCustomerAsync(Room room, Message message, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(room.clientConnectionId)) return Task.CompletedTask;
        return _hub.Clients.Client(room.clientConnectionId).SendAsync("MessageReceived", message, ct);
    }
}
