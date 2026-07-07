using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

/// <summary>
/// A customer email conversation in the digital-workspace inbox. Created by the
/// EmailInboundWorker when a message arrives that doesn't match an existing thread
/// (by References/In-Reply-To, falling back to normalized subject + counterpart).
/// Aggregate fields (snippet, counts) are denormalized for the inbox list view.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class EmailThread : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("subject")] public string Subject { get; set; } = string.Empty;

    /// <summary>Subject lowercased with Re:/Fwd: prefixes stripped — used for fallback thread matching.</summary>
    [BsonElement("normalizedSubject")] public string NormalizedSubject { get; set; } = string.Empty;

    // The customer side of the conversation.
    [BsonElement("counterpartName")] public string CounterpartName { get; set; } = string.Empty;
    [BsonElement("counterpartEmail")] public string CounterpartEmail { get; set; } = string.Empty;

    /// <summary>The mailbox this conversation belongs to (our side).</summary>
    [BsonElement("mailbox")] public string Mailbox { get; set; } = string.Empty;

    [BsonElement("lastMessageAt")] public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    [BsonElement("lastMessageSnippet")] public string LastMessageSnippet { get; set; } = string.Empty;
    [BsonElement("lastMessageDirection")] public string LastMessageDirection { get; set; } = "inbound";
    [BsonElement("lastMessageHasAttachments")] public bool LastMessageHasAttachments { get; set; }
    [BsonElement("messageCount")] public int MessageCount { get; set; }
    [BsonElement("unreadCount")] public int UnreadCount { get; set; }

    [BsonElement("status")] public string Status { get; set; } = "open"; // open | resolved | archived
    [BsonElement("assignedTo")] [BsonIgnoreIfNull] public string? AssignedTo { get; set; }
    [BsonElement("resolvedBy")] [BsonIgnoreIfNull] public string? ResolvedBy { get; set; }
    [BsonElement("resolvedAt")] [BsonIgnoreIfNull] public DateTime? ResolvedAt { get; set; }

    /// <summary>Hidden from the inbox until this time; new inbound mail clears it.</summary>
    [BsonElement("snoozedUntil")] [BsonIgnoreIfNull] public DateTime? SnoozedUntil { get; set; }

    [BsonElement("starred")] public bool Starred { get; set; }
}
