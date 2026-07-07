using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application.Abstractions;

public interface IChannelAdapter
{
    string ChannelKey { get; }
    Task SendToCustomerAsync(Room room, Message message, CancellationToken ct = default);
}
