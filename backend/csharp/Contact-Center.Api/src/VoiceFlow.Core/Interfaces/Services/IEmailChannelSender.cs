namespace VoiceFlow.Core.Interfaces.Services;

public sealed record OutboundAttachment(string FileName, string ContentType, byte[] Content);

/// <summary>An outgoing channel email (agent reply or new compose).</summary>
/// <param name="FromMailbox">Sending account address; resolved against configured accounts.</param>
/// <param name="InReplyToMessageId">RFC id of the customer email being answered (null for new compose).</param>
/// <param name="References">Existing References chain, oldest first.</param>
public sealed record OutboundEmail(
    string FromMailbox,
    string ToEmail,
    string ToName,
    IReadOnlyList<string> Cc,
    string Subject,
    string TextBody,
    string? HtmlBody,
    string? InReplyToMessageId,
    IReadOnlyList<string> References,
    IReadOnlyList<OutboundAttachment> Attachments);

/// <summary>Sends channel emails over SMTP with correct RFC threading headers.</summary>
public interface IEmailChannelSender
{
    /// <returns>The RFC message id assigned to the outgoing email (no angle brackets).</returns>
    Task<string> SendAsync(OutboundEmail email, CancellationToken cancellationToken = default);

    /// <summary>The configured account address to send from (requested, or the default account).</summary>
    string ResolveMailbox(string? requested);
}
