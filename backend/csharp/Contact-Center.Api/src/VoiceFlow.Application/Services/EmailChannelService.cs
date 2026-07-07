using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Email;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Application.Services;

public sealed class EmailChannelService : IEmailChannelService
{
    private const long MaxAttachmentBytes = 20 * 1024 * 1024; // Gmail caps outgoing mail at 25 MB

    private readonly IEmailThreadRepository _threads;
    private readonly IEmailMessageRepository _messages;
    private readonly IEmailChannelSender _sender;
    private readonly IEmailAttachmentFetcher _attachments;
    private readonly IEmailAgentSettingsRepository _agentSettings;
    private readonly IProfileRepository _profiles;

    public EmailChannelService(
        IEmailThreadRepository threads,
        IEmailMessageRepository messages,
        IEmailChannelSender sender,
        IEmailAttachmentFetcher attachments,
        IEmailAgentSettingsRepository agentSettings,
        IProfileRepository profiles)
    {
        _threads = threads;
        _messages = messages;
        _sender = sender;
        _attachments = attachments;
        _agentSettings = agentSettings;
        _profiles = profiles;
    }

    public async Task<IReadOnlyList<EmailThreadResponse>> ListThreadsAsync(
        string tenantId, string? status, CancellationToken cancellationToken = default)
    {
        var items = await _threads.ListAsync(tenantId, status, cancellationToken);
        return items.Select(MapThread).ToList();
    }

    public async Task<Result<IReadOnlyList<EmailMessageResponse>>> ListMessagesAsync(
        string tenantId, string threadId, CancellationToken cancellationToken = default)
    {
        var thread = await GetAccessibleThreadAsync(tenantId, threadId, cancellationToken);
        if (thread is null)
            return Result.Failure<IReadOnlyList<EmailMessageResponse>>(Error.NotFound("EmailThread", threadId));

        var items = await _messages.ListByThreadAsync(threadId, cancellationToken);
        return Result.Success<IReadOnlyList<EmailMessageResponse>>(items.Select(MapMessage).ToList());
    }

    public async Task<Result<EmailMessageResponse>> SendReplyAsync(
        string tenantId, string agentId, string threadId, SendEmailReplyRequest request,
        CancellationToken cancellationToken = default)
    {
        var thread = await GetAccessibleThreadAsync(tenantId, threadId, cancellationToken);
        if (thread is null)
            return Result.Failure<EmailMessageResponse>(Error.NotFound("EmailThread", threadId));

        if (string.IsNullOrWhiteSpace(request.Body))
            return Result.Failure<EmailMessageResponse>(Error.Validation("Body", "Reply body is empty."));

        var agentName = await AgentNameAsync(agentId, tenantId, cancellationToken);
        var latestInbound = await _messages.GetLatestInboundAsync(threadId, cancellationToken);

        var subject = thread.Subject.StartsWith("re:", StringComparison.OrdinalIgnoreCase)
            ? thread.Subject
            : $"Re: {thread.Subject}";

        // References = prior chain + the message being answered (RFC 5322 §3.6.4).
        var references = new List<string>(latestInbound?.References ?? []);
        if (latestInbound is not null && !references.Contains(latestInbound.MessageId))
            references.Add(latestInbound.MessageId);

        var cc = CleanAddresses(request.Cc);

        var attachmentsResult = DecodeAttachments(request.Attachments);
        if (attachmentsResult.IsFailure)
            return Result.Failure<EmailMessageResponse>(attachmentsResult.Error);
        var attachments = attachmentsResult.Value;

        string messageId;
        try
        {
            messageId = await _sender.SendAsync(new OutboundEmail(
                thread.Mailbox,
                thread.CounterpartEmail,
                thread.CounterpartName,
                cc,
                subject,
                request.Body,
                request.HtmlBody,
                latestInbound?.MessageId,
                references,
                attachments), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<EmailMessageResponse>(Error.Validation("Email", ex.Message));
        }

        var message = new EmailMessage
        {
            TenantId = thread.TenantId,
            ThreadId = thread.Id,
            Direction = "outbound",
            MessageId = messageId,
            InReplyTo = latestInbound?.MessageId,
            References = references,
            FromName = agentName,
            FromEmail = thread.Mailbox,
            ToEmail = thread.CounterpartEmail,
            CcEmails = cc,
            Subject = subject,
            TextBody = request.Body,
            HtmlBody = request.HtmlBody,
            AttachmentNames = attachments.Select(a => a.FileName).ToList(),
            SentAt = DateTime.UtcNow,
            AgentId = agentId,
            AgentName = agentName,
        };
        await _messages.InsertAsync(message, cancellationToken);

        await _threads.ApplyNewMessageAsync(
            thread.Id, Snippet(request.Body), message.SentAt, "outbound", hasAttachments: attachments.Count > 0, cancellationToken);

        return Result.Success(MapMessage(message));
    }

    public async Task<Result<EmailThreadResponse>> ComposeAsync(
        string tenantId, string agentId, ComposeEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.To) || string.IsNullOrWhiteSpace(request.Subject) ||
            string.IsNullOrWhiteSpace(request.Body))
            return Result.Failure<EmailThreadResponse>(Error.Validation("Compose", "To, Subject and Body are required."));

        var agentName = await AgentNameAsync(agentId, tenantId, cancellationToken);
        var toName = string.IsNullOrWhiteSpace(request.ToName) ? request.To : request.ToName.Trim();
        var cc = CleanAddresses(request.Cc);

        var attachmentsResult = DecodeAttachments(request.Attachments);
        if (attachmentsResult.IsFailure)
            return Result.Failure<EmailThreadResponse>(attachmentsResult.Error);
        var attachments = attachmentsResult.Value;

        string mailbox;
        string messageId;
        try
        {
            mailbox = _sender.ResolveMailbox(request.Mailbox);
            messageId = await _sender.SendAsync(new OutboundEmail(
                mailbox,
                request.To.Trim(),
                toName,
                cc,
                request.Subject.Trim(),
                request.Body,
                request.HtmlBody,
                InReplyToMessageId: null,
                References: [],
                Attachments: attachments), cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<EmailThreadResponse>(Error.Validation("Email", ex.Message));
        }

        var now = DateTime.UtcNow;
        var thread = new EmailThread
        {
            TenantId = tenantId,
            Subject = request.Subject.Trim(),
            NormalizedSubject = NormalizeSubject(request.Subject),
            CounterpartName = toName,
            CounterpartEmail = request.To.Trim(),
            Mailbox = mailbox,
            LastMessageAt = now,
            LastMessageSnippet = Snippet(request.Body),
            LastMessageDirection = "outbound",
            LastMessageHasAttachments = attachments.Count > 0,
            MessageCount = 1,
            AssignedTo = agentId,
        };
        await _threads.InsertAsync(thread, cancellationToken);

        var message = new EmailMessage
        {
            TenantId = tenantId,
            ThreadId = thread.Id,
            Direction = "outbound",
            MessageId = messageId,
            FromName = agentName,
            FromEmail = mailbox,
            ToEmail = request.To.Trim(),
            CcEmails = cc,
            Subject = thread.Subject,
            TextBody = request.Body,
            HtmlBody = request.HtmlBody,
            AttachmentNames = attachments.Select(a => a.FileName).ToList(),
            SentAt = now,
            AgentId = agentId,
            AgentName = agentName,
        };
        await _messages.InsertAsync(message, cancellationToken);

        return Result.Success(MapThread(thread));
    }

    public Task<Result> MarkReadAsync(string tenantId, string threadId, CancellationToken cancellationToken = default)
        => WithThreadAsync(tenantId, threadId, t => _threads.MarkReadAsync(t.Id, cancellationToken), cancellationToken);

    public Task<Result> MarkUnreadAsync(string tenantId, string threadId, CancellationToken cancellationToken = default)
        => WithThreadAsync(tenantId, threadId, t => _threads.MarkUnreadAsync(t.Id, cancellationToken), cancellationToken);

    public Task<Result> ResolveAsync(string tenantId, string agentId, string threadId, CancellationToken cancellationToken = default)
        => WithThreadAsync(tenantId, threadId, t => _threads.ResolveAsync(t.Id, agentId, cancellationToken), cancellationToken);

    public Task<Result> ReopenAsync(string tenantId, string threadId, CancellationToken cancellationToken = default)
        => WithThreadAsync(tenantId, threadId, t => _threads.SetStatusAsync(t.Id, "open", cancellationToken), cancellationToken);

    public Task<Result> ArchiveAsync(string tenantId, string threadId, CancellationToken cancellationToken = default)
        => WithThreadAsync(tenantId, threadId, t => _threads.SetStatusAsync(t.Id, "archived", cancellationToken), cancellationToken);

    public Task<Result> SnoozeAsync(string tenantId, string threadId, DateTime? until, CancellationToken cancellationToken = default)
        => WithThreadAsync(tenantId, threadId, t => _threads.SetSnoozeAsync(t.Id, until, cancellationToken), cancellationToken);

    public Task<Result> StarAsync(string tenantId, string threadId, bool starred, CancellationToken cancellationToken = default)
        => WithThreadAsync(tenantId, threadId, t => _threads.SetStarredAsync(t.Id, starred, cancellationToken), cancellationToken);

    public async Task<Result<EmailAttachmentContent>> GetAttachmentAsync(
        string tenantId, string messageId, int attachmentIndex, CancellationToken cancellationToken = default)
    {
        var message = await _messages.GetByIdAsync(messageId, cancellationToken);
        if (message is null || (message.TenantId.Length > 0 && message.TenantId != tenantId))
            return Result.Failure<EmailAttachmentContent>(Error.NotFound("EmailMessage", messageId));

        if (message.Direction != "inbound" || message.ImapUid is null)
            return Result.Failure<EmailAttachmentContent>(Error.Validation("Attachment", "This message has no fetchable attachments."));

        var content = await _attachments.FetchAsync(message.ToEmail, message.ImapFolder, message.ImapUid.Value, attachmentIndex, cancellationToken);
        if (content is null)
            return Result.Failure<EmailAttachmentContent>(Error.NotFound("EmailAttachment", $"{messageId}/{attachmentIndex}"));

        return Result.Success(content);
    }

    public async Task<EmailSignatureResponse> GetSignatureAsync(
        string tenantId, string agentId, CancellationToken cancellationToken = default)
    {
        var settings = await _agentSettings.GetByAgentAsync(tenantId, agentId, cancellationToken);
        return new EmailSignatureResponse { Html = settings?.SignatureHtml ?? string.Empty };
    }

    public Task SetSignatureAsync(string tenantId, string agentId, string html, CancellationToken cancellationToken = default)
        => _agentSettings.UpsertByAgentAsync(tenantId, agentId, html ?? string.Empty, cancellationToken);

    private static Result<List<OutboundAttachment>> DecodeAttachments(List<EmailAttachmentUpload> uploads)
    {
        var result = new List<OutboundAttachment>();
        long total = 0;

        foreach (var upload in uploads)
        {
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(upload.Base64Content);
            }
            catch (FormatException)
            {
                return Result.Failure<List<OutboundAttachment>>(
                    Error.Validation("Attachments", $"Attachment '{upload.FileName}' is not valid base64."));
            }

            total += bytes.LongLength;
            if (total > MaxAttachmentBytes)
                return Result.Failure<List<OutboundAttachment>>(
                    Error.Validation("Attachments", "Attachments exceed the 20 MB total limit."));

            result.Add(new OutboundAttachment(upload.FileName, upload.ContentType, bytes));
        }

        return Result.Success(result);
    }

    private async Task<Result> WithThreadAsync(
        string tenantId, string threadId, Func<EmailThread, Task> action, CancellationToken ct)
    {
        var thread = await GetAccessibleThreadAsync(tenantId, threadId, ct);
        if (thread is null)
            return Result.Failure(Error.NotFound("EmailThread", threadId));

        await action(thread);
        return Result.Success();
    }

    private async Task<string> AgentNameAsync(string agentId, string tenantId, CancellationToken ct)
    {
        var profile = await _profiles.GetByUserIdAndTenantAsync(agentId, tenantId, ct);
        return profile?.DisplayName ?? "Agent";
    }

    /// <summary>
    /// Threads stamped with an empty tenant id (mailbox not mapped to a tenant in config)
    /// are visible to every tenant — single-org deployments work without extra setup.
    /// </summary>
    private async Task<EmailThread?> GetAccessibleThreadAsync(string tenantId, string threadId, CancellationToken ct)
    {
        var thread = await _threads.GetByIdAsync(threadId, ct);
        if (thread is null) return null;
        if (thread.TenantId.Length > 0 && thread.TenantId != tenantId) return null;
        return thread;
    }

    private static List<string> CleanAddresses(IEnumerable<string> addresses) =>
        addresses.Select(a => a.Trim()).Where(a => a.Contains('@')).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static string NormalizeSubject(string subject)
    {
        var s = subject.Trim();
        while (true)
        {
            var m = System.Text.RegularExpressions.Regex.Match(
                s, @"^(re|fw|fwd)\s*:\s*", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (!m.Success) break;
            s = s[m.Length..].Trim();
        }
        return s.ToLowerInvariant();
    }

    private static string Snippet(string body)
    {
        var text = body.ReplaceLineEndings(" ").Trim();
        return text.Length <= 140 ? text : text[..140];
    }

    private static EmailThreadResponse MapThread(EmailThread t) => new()
    {
        Id = t.Id,
        Subject = t.Subject,
        CounterpartName = t.CounterpartName,
        CounterpartEmail = t.CounterpartEmail,
        Mailbox = t.Mailbox,
        LastMessageAt = t.LastMessageAt,
        LastMessageSnippet = t.LastMessageSnippet,
        LastMessageDirection = t.LastMessageDirection,
        LastMessageHasAttachments = t.LastMessageHasAttachments,
        MessageCount = t.MessageCount,
        UnreadCount = t.UnreadCount,
        Status = t.Status,
        AssignedTo = t.AssignedTo,
        SnoozedUntil = t.SnoozedUntil,
        Starred = t.Starred,
    };

    private static EmailMessageResponse MapMessage(EmailMessage m) => new()
    {
        Id = m.Id,
        ThreadId = m.ThreadId,
        Direction = m.Direction,
        FromName = m.FromName,
        FromEmail = m.FromEmail,
        ToEmail = m.ToEmail,
        CcEmails = m.CcEmails,
        Subject = m.Subject,
        TextBody = m.TextBody,
        HtmlBody = m.HtmlBody,
        AttachmentNames = m.AttachmentNames,
        SentAt = m.SentAt,
        AgentId = m.AgentId,
        AgentName = m.AgentName,
    };
}
