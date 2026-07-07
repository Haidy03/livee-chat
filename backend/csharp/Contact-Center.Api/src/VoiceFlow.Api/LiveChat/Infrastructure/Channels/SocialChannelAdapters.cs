using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Channels;

public sealed class WhatsAppChannelAdapter : IChannelAdapter
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<WhatsAppChannelAdapter> _log;
    public WhatsAppChannelAdapter(IHttpClientFactory http, ILogger<WhatsAppChannelAdapter> log)
    { _http = http; _log = log; }
    public string ChannelKey => "whatsapp";

    public async Task SendToCustomerAsync(Room room, Message message, CancellationToken ct = default)
    {
        var client = _http.CreateClient("whatsapp");
        var payload = new
        {
            messaging_product = "whatsapp",
            to = room.contactId,
            type = "text",
            text = new { body = message.Text },
        };
        try
        {
            using var resp = await client.PostAsJsonAsync("messages", payload, ct);
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex) { _log.LogWarning(ex, "WhatsApp send failed for {ConvId}", room._id); }
    }
}

public sealed class MessengerChannelAdapter : IChannelAdapter
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<MessengerChannelAdapter> _log;
    public MessengerChannelAdapter(IHttpClientFactory http, ILogger<MessengerChannelAdapter> log)
    { _http = http; _log = log; }
    public string ChannelKey => "messenger";

    public async Task SendToCustomerAsync(Room room, Message message, CancellationToken ct = default)
    {
        var client = _http.CreateClient("messenger");
        var payload = new
        {
            recipient = new { id = room.contactId },
            message = new { text = message.Text },
        };
        try
        {
            using var resp = await client.PostAsJsonAsync("me/messages", payload, ct);
            resp.EnsureSuccessStatusCode();
        }
        catch (Exception ex) { _log.LogWarning(ex, "Messenger send failed for {ConvId}", room._id); }
    }
}
