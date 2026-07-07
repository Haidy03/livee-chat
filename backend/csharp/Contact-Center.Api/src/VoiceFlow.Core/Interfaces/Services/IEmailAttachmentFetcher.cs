namespace VoiceFlow.Core.Interfaces.Services;

public sealed record EmailAttachmentContent(Stream Content, string ContentType, string FileName);

/// <summary>
/// Fetches an inbound email's attachment on demand from the mailbox over IMAP.
/// Attachments are never copied to our own storage — Gmail keeps the original message;
/// we address it by the IMAP UID recorded at ingest time.
/// </summary>
public interface IEmailAttachmentFetcher
{
    /// <returns>Null when the message or attachment no longer exists in the mailbox.</returns>
    Task<EmailAttachmentContent?> FetchAsync(string mailbox, string folder, long imapUid, int attachmentIndex, CancellationToken cancellationToken = default);
}
