using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Email;
using VoiceFlow.Infrastructure.ExternalServices;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Api.Controllers;

/// <summary>
/// Digital-workspace email inbox. Ingest happens out-of-band: the EmailInboundWorker
/// polls the channel mailboxes over IMAP and stores threads/messages. This controller is
/// the authenticated read/reply/compose side used by the workspace UI.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/email")]
public sealed class EmailChannelController : ControllerBase
{
    private readonly IEmailChannelService _service;
    private readonly ICurrentUser _currentUser;
    private readonly IOptions<EmailOptions> _emailOptions;

    public EmailChannelController(
        IEmailChannelService service,
        ICurrentUser currentUser,
        IOptions<EmailOptions> emailOptions)
    {
        _service = service;
        _currentUser = currentUser;
        _emailOptions = emailOptions;
    }

    [HttpGet("mailboxes")]
    public IActionResult Mailboxes()
    {
        var accounts = SmtpEmailChannelSender.Accounts(_emailOptions.Value)
            .Select(a => new EmailMailboxResponse
            {
                Address = a.Username,
                DisplayName = string.IsNullOrWhiteSpace(a.DisplayName) ? _emailOptions.Value.FromName : a.DisplayName,
            })
            .ToList();
        return Ok(ApiResponse<IReadOnlyList<EmailMailboxResponse>>.Ok(accounts));
    }

    [HttpGet("threads")]
    public async Task<IActionResult> ListThreads([FromQuery] string? status, CancellationToken ct)
    {
        var items = await _service.ListThreadsAsync(_currentUser.TenantId, status, ct);
        return Ok(ApiResponse<IReadOnlyList<EmailThreadResponse>>.Ok(items));
    }

    [HttpGet("threads/{id}/messages")]
    public async Task<IActionResult> ListMessages(string id, CancellationToken ct)
    {
        var result = await _service.ListMessagesAsync(_currentUser.TenantId, id, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse.Fail(result.Error.Description));
        return Ok(ApiResponse<IReadOnlyList<EmailMessageResponse>>.Ok(result.Value));
    }

    [HttpPost("compose")]
    [RequestSizeLimit(30 * 1024 * 1024)] // base64-encoded attachments up to the 20 MB decoded cap
    public async Task<IActionResult> Compose([FromBody] ComposeEmailRequest request, CancellationToken ct)
    {
        var result = await _service.ComposeAsync(_currentUser.TenantId, _currentUser.UserId, request, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse.Fail(result.Error.Description));
        return Ok(ApiResponse<EmailThreadResponse>.Ok(result.Value));
    }

    [HttpPost("threads/{id}/reply")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Reply(string id, [FromBody] SendEmailReplyRequest request, CancellationToken ct)
    {
        var result = await _service.SendReplyAsync(_currentUser.TenantId, _currentUser.UserId, id, request, ct);
        if (result.IsFailure)
            return BadRequest(ApiResponse.Fail(result.Error.Description));
        return Ok(ApiResponse<EmailMessageResponse>.Ok(result.Value));
    }

    [HttpPost("threads/{id}/read")]
    public Task<IActionResult> MarkRead(string id, CancellationToken ct)
        => Simple(() => _service.MarkReadAsync(_currentUser.TenantId, id, ct));

    [HttpPost("threads/{id}/unread")]
    public Task<IActionResult> MarkUnread(string id, CancellationToken ct)
        => Simple(() => _service.MarkUnreadAsync(_currentUser.TenantId, id, ct));

    [HttpPost("threads/{id}/resolve")]
    public Task<IActionResult> Resolve(string id, CancellationToken ct)
        => Simple(() => _service.ResolveAsync(_currentUser.TenantId, _currentUser.UserId, id, ct));

    [HttpPost("threads/{id}/reopen")]
    public Task<IActionResult> Reopen(string id, CancellationToken ct)
        => Simple(() => _service.ReopenAsync(_currentUser.TenantId, id, ct));

    [HttpPost("threads/{id}/archive")]
    public Task<IActionResult> Archive(string id, CancellationToken ct)
        => Simple(() => _service.ArchiveAsync(_currentUser.TenantId, id, ct));

    [HttpPost("threads/{id}/snooze")]
    public Task<IActionResult> Snooze(string id, [FromBody] SnoozeEmailThreadRequest request, CancellationToken ct)
        => Simple(() => _service.SnoozeAsync(_currentUser.TenantId, id, request.Until, ct));

    [HttpPost("threads/{id}/star")]
    public Task<IActionResult> Star(string id, [FromBody] StarEmailThreadRequest request, CancellationToken ct)
        => Simple(() => _service.StarAsync(_currentUser.TenantId, id, request.Starred, ct));

    [HttpGet("signature")]
    public async Task<IActionResult> GetSignature(CancellationToken ct)
    {
        var signature = await _service.GetSignatureAsync(_currentUser.TenantId, _currentUser.UserId, ct);
        return Ok(ApiResponse<EmailSignatureResponse>.Ok(signature));
    }

    [HttpPut("signature")]
    public async Task<IActionResult> SetSignature([FromBody] UpdateEmailSignatureRequest request, CancellationToken ct)
    {
        await _service.SetSignatureAsync(_currentUser.TenantId, _currentUser.UserId, request.Html, ct);
        return Ok(ApiResponse.Ok());
    }

    [HttpGet("messages/{id}/attachments/{index:int}")]
    public async Task<IActionResult> Attachment(string id, int index, CancellationToken ct)
    {
        var result = await _service.GetAttachmentAsync(_currentUser.TenantId, id, index, ct);
        if (result.IsFailure)
            return NotFound(ApiResponse.Fail(result.Error.Description));

        var a = result.Value;
        return File(a.Content, a.ContentType, a.FileName);
    }

    private async Task<IActionResult> Simple(Func<Task<VoiceFlow.Core.Common.Result>> action)
    {
        var result = await action();
        if (result.IsFailure)
            return NotFound(ApiResponse.Fail(result.Error.Description));
        return Ok(ApiResponse.Ok());
    }
}
