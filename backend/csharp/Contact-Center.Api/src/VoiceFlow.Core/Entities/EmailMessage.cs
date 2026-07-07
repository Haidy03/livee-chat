using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

/// <summary>
/// A single email inside an <see cref="EmailThread"/>. Inbound messages are written by
/// the EmailInboundWorker (IMAP); outbound ones by EmailChannelService after an agent
/// reply is accepted by SMTP. RFC 5322 ids (MessageId/InReplyTo/References) are kept so
/// replies thread correctly in the customer's mail client and future inbound mail can be
/// matched back to its thread.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class EmailMessage : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("threadId")] public string ThreadId { get; set; } = string.Empty;

    [BsonElement("direction")] public string Direction { get; set; } = "inbound"; // inbound | outbound

    // RFC 5322 message ids (angle brackets stripped).
    [BsonElement("messageId")] public string MessageId { get; set; } = string.Empty;
    [BsonElement("inReplyTo")] [BsonIgnoreIfNull] public string? InReplyTo { get; set; }
    [BsonElement("references")] public List<string> References { get; set; } = [];

    [BsonElement("fromName")] public string FromName { get; set; } = string.Empty;
    [BsonElement("fromEmail")] public string FromEmail { get; set; } = string.Empty;
    [BsonElement("toEmail")] public string ToEmail { get; set; } = string.Empty;
    [BsonElement("ccEmails")] public List<string> CcEmails { get; set; } = [];

    [BsonElement("subject")] public string Subject { get; set; } = string.Empty;
    [BsonElement("textBody")] public string TextBody { get; set; } = string.Empty;
    [BsonElement("htmlBody")] [BsonIgnoreIfNull] public string? HtmlBody { get; set; }

    [BsonElement("attachmentNames")] public List<string> AttachmentNames { get; set; } = [];

    [BsonElement("sentAt")] public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>Agent profile id for outbound replies; null for inbound customer mail.</summary>
    [BsonElement("agentId")] [BsonIgnoreIfNull] public string? AgentId { get; set; }
    [BsonElement("agentName")] [BsonIgnoreIfNull] public string? AgentName { get; set; }

    /// <summary>IMAP UID of the source message (inbound only) — used to fetch attachments on demand.</summary>
    [BsonElement("imapUid")] [BsonIgnoreIfNull] public long? ImapUid { get; set; }

    /// <summary>IMAP folder the message was ingested from (UIDs are per-folder).</summary>
    [BsonElement("imapFolder")] public string ImapFolder { get; set; } = "INBOX";
}
