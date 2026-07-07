using MailKit;
using MailKit.Net.Imap;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Infrastructure.ExternalServices;

/// <summary>
/// Streams an attachment out of the original message in the Gmail mailbox (by IMAP UID).
/// No copy is stored on our side — the mailbox itself is the file store.
/// </summary>
public sealed class ImapEmailAttachmentFetcher : IEmailAttachmentFetcher
{
    private readonly EmailOptions _options;

    public ImapEmailAttachmentFetcher(IOptions<EmailOptions> options)
    {
        _options = options.Value;
    }

    public async Task<EmailAttachmentContent?> FetchAsync(
        string mailbox, string folder, long imapUid, int attachmentIndex, CancellationToken cancellationToken = default)
    {
        var account = SmtpEmailChannelSender.Accounts(_options)
            .FirstOrDefault(a => string.Equals(a.Username, mailbox, StringComparison.OrdinalIgnoreCase));
        if (account is null) return null;

        using var client = new ImapClient();
        await client.ConnectAsync(_options.Inbound.ImapHost, _options.Inbound.ImapPort, SecureSocketOptions.SslOnConnect, cancellationToken);
        await client.AuthenticateAsync(account.Username, account.Password, cancellationToken);

        IMailFolder mailFolder;
        try
        {
            mailFolder = string.IsNullOrWhiteSpace(folder) || string.Equals(folder, "INBOX", StringComparison.OrdinalIgnoreCase)
                ? client.Inbox
                : await client.GetFolderAsync(folder, cancellationToken);
        }
        catch (FolderNotFoundException)
        {
            return null;
        }
        await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

        MimeMessage mime;
        try
        {
            mime = await mailFolder.GetMessageAsync(new UniqueId((uint)imapUid), cancellationToken);
        }
        catch (MessageNotFoundException)
        {
            return null;
        }

        var attachments = mime.Attachments.ToList();
        if (attachmentIndex < 0 || attachmentIndex >= attachments.Count) return null;

        var entity = attachments[attachmentIndex];
        var stream = new MemoryStream();

        if (entity is MimePart part && part.Content is not null)
        {
            await part.Content.DecodeToAsync(stream, cancellationToken);
            stream.Position = 0;
            return new EmailAttachmentContent(
                stream,
                part.ContentType.MimeType ?? "application/octet-stream",
                part.FileName ?? "attachment");
        }

        // e.g. an attached email (message/rfc822)
        await entity.WriteToAsync(stream, cancellationToken);
        stream.Position = 0;
        return new EmailAttachmentContent(stream, "message/rfc822", "attached-message.eml");
    }
}
