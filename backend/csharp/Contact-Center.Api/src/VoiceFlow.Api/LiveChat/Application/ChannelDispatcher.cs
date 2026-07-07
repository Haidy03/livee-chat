using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application;

public sealed class ChannelDispatcher
{
    private readonly Dictionary<string, IChannelAdapter> _adapters;

    public ChannelDispatcher(IEnumerable<IChannelAdapter> adapters)
    {
        _adapters = adapters.ToDictionary(a => a.ChannelKey, StringComparer.OrdinalIgnoreCase);
    }

    public Task SendAsync(Room room, Message message, CancellationToken ct = default)
    {
        if (_adapters.TryGetValue(room.channel ?? string.Empty, out var adapter))
            return adapter.SendToCustomerAsync(room, message, ct);
        return Task.CompletedTask;
    }
}
