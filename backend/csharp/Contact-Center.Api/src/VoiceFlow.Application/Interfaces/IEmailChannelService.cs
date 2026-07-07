using VoiceFlow.Contracts.Email;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Application.Interfaces;

public interface IEmailChannelService
{
    Task<IReadOnlyList<EmailThreadResponse>> ListThreadsAsync(string tenantId, string? status, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<EmailMessageResponse>>> ListMessagesAsync(string tenantId, string threadId, CancellationToken cancellationToken = default);

    Task<Result<EmailMessageResponse>> SendReplyAsync(string tenantId, string agentId, string threadId, SendEmailReplyRequest request, CancellationToken cancellationToken = default);

    /// <summary>Starts a brand-new outbound conversation; returns the created thread.</summary>
    Task<Result<EmailThreadResponse>> ComposeAsync(string tenantId, string agentId, ComposeEmailRequest request, CancellationToken cancellationToken = default);

    Task<Result> MarkReadAsync(string tenantId, string threadId, CancellationToken cancellationToken = default);

    Task<Result> MarkUnreadAsync(string tenantId, string threadId, CancellationToken cancellationToken = default);

    Task<Result> ResolveAsync(string tenantId, string agentId, string threadId, CancellationToken cancellationToken = default);

    /// <summary>Back to open — used for reopen (from resolved) and unarchive.</summary>
    Task<Result> ReopenAsync(string tenantId, string threadId, CancellationToken cancellationToken = default);

    Task<Result> ArchiveAsync(string tenantId, string threadId, CancellationToken cancellationToken = default);

    Task<Result> SnoozeAsync(string tenantId, string threadId, DateTime? until, CancellationToken cancellationToken = default);

    Task<Result> StarAsync(string tenantId, string threadId, bool starred, CancellationToken cancellationToken = default);

    /// <summary>Streams an inbound attachment from the mailbox itself (IMAP) — no local storage.</summary>
    Task<Result<EmailAttachmentContent>> GetAttachmentAsync(string tenantId, string messageId, int attachmentIndex, CancellationToken cancellationToken = default);

    Task<EmailSignatureResponse> GetSignatureAsync(string tenantId, string agentId, CancellationToken cancellationToken = default);

    Task SetSignatureAsync(string tenantId, string agentId, string html, CancellationToken cancellationToken = default);
}
