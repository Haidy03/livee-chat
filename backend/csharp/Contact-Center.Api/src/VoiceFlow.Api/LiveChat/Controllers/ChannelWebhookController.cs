using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Api.LiveChat.Application;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Application.Dtos;
using VoiceFlow.Api.LiveChat.Config;
using Microsoft.Extensions.Options;

namespace VoiceFlow.Api.LiveChat.Controllers;

[ApiController]
[Route("webhooks")]
public class ChannelWebhookController : ControllerBase
{
    private readonly ClientRequestService _requests;
    private readonly IRoomRepository _rooms;
    private readonly RoomService _roomService;
    private readonly LiveChatOptions _options;

    public ChannelWebhookController(
        ClientRequestService requests,
        IRoomRepository rooms,
        RoomService roomService,
        IOptions<LiveChatOptions> options)
    {
        _requests = requests;
        _rooms = rooms;
        _roomService = roomService;
        _options = options.Value;
    }

    // Meta verify handshake
    [HttpGet("whatsapp")]
    public IActionResult VerifyWhatsApp(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.challenge")] string? challenge,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken)
    {
        var expected = _options.Channels?.WhatsApp?.VerifyToken;
        if (!string.IsNullOrEmpty(expected) && !string.Equals(verifyToken, expected, StringComparison.Ordinal))
            return Forbid();
        return Content(challenge ?? string.Empty);
    }

    public class InboundWebhookMessage
    {
        public string Channel { get; set; } = string.Empty;
        public string ContactId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string DepartmentId { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string Lang { get; set; } = "en";
    }

    [HttpPost("whatsapp")]
    public Task<IActionResult> WhatsApp([FromBody] InboundWebhookMessage msg, CancellationToken ct)
        => IngestAsync("whatsapp", msg, ct);

    [HttpPost("messenger")]
    public Task<IActionResult> Messenger([FromBody] InboundWebhookMessage msg, CancellationToken ct)
        => IngestAsync("messenger", msg, ct);

    private async Task<IActionResult> IngestAsync(string channel, InboundWebhookMessage msg, CancellationToken ct)
    {
        if (msg is null || string.IsNullOrWhiteSpace(msg.ContactId)) return BadRequest();

        var existing = await _rooms.GetActiveByChannelContactAsync(channel, msg.ContactId, ct);
        if (existing is not null)
        {
            await _roomService.HandleInboundMessageAsync(existing._id, msg.Text, ct);
            return Ok(new { roomId = existing._id });
        }

        var contact = new NewContact
        {
            Channel = channel,
            ContactId = msg.ContactId,
            Lang = msg.Lang,
            ClientInfo = msg.Text,
            ConnectionId = string.Empty,
            Department = new Domain.DepartmentInfo { id = msg.DepartmentId, name = msg.DepartmentName },
        };
        var reqId = await _requests.CreateRequestAsync(contact, ct);
        return Ok(new { requestId = reqId });
    }
}
