using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

/// <summary>
/// A recorded voicemail message. Written by the FreeSwitchXmlCurl webhook receiver
/// (status "new"), enriched by the ContactCenterService worker (s3Url + transcript),
/// and served to agents by the inbox API. Owner (queue/group/agent) drives who can see
/// it; the caller is a separate field. Field names mirror
/// FreeSwitchXmlCurl.Models.VoicemailMessage so all three services share one collection.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class Voicemail : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("flowId")] [BsonIgnoreIfNull] public string? FlowId { get; set; }
    [BsonElement("nodeId")] [BsonIgnoreIfNull] public string? NodeId { get; set; }

    // Ownership — WHO CALLS BACK (mailbox owner). Drives the audience. Never the caller.
    [BsonElement("ownerType")] public string OwnerType { get; set; } = "flow"; // queue | group | agent | flow
    [BsonElement("ownerId")] public string OwnerId { get; set; } = string.Empty;

    [BsonElement("mailbox")] [BsonIgnoreIfNull] public string? Mailbox { get; set; }

    // The customer who left the message.
    [BsonElement("callerIdNumber")] [BsonIgnoreIfNull] public string? CallerIdNumber { get; set; }
    [BsonElement("destinationNumber")] [BsonIgnoreIfNull] public string? DestinationNumber { get; set; }

    [BsonElement("uuid")] [BsonIgnoreIfNull] public string? Uuid { get; set; }
    [BsonElement("recordingPath")] [BsonIgnoreIfNull] public string? RecordingPath { get; set; }
    [BsonElement("format")] public string Format { get; set; } = "wav";
    [BsonElement("durationSeconds")] public int DurationSeconds { get; set; }
    [BsonElement("timestamp")] public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Worker-filled after S3 upload + optional transcription.
    [BsonElement("transcriptionRequested")] public bool TranscriptionRequested { get; set; }
    [BsonElement("s3Url")] [BsonIgnoreIfNull] public string? S3Url { get; set; }
    [BsonElement("transcript")] [BsonIgnoreIfNull] public string? Transcript { get; set; }
    [BsonElement("summary")] [BsonIgnoreIfNull] public string? Summary { get; set; }
    [BsonElement("sentiment")] [BsonIgnoreIfNull] public string? Sentiment { get; set; }

    // Lifecycle — owned by the app, not the engine.
    [BsonElement("status")] public string Status { get; set; } = "new"; // new | claimed | done
    [BsonElement("claimedBy")] [BsonIgnoreIfNull] public string? ClaimedBy { get; set; }
    [BsonElement("claimedAt")] [BsonIgnoreIfNull] public DateTime? ClaimedAt { get; set; }
    [BsonElement("resolvedBy")] [BsonIgnoreIfNull] public string? ResolvedBy { get; set; }
    [BsonElement("resolvedAt")] [BsonIgnoreIfNull] public DateTime? ResolvedAt { get; set; }
    [BsonElement("escalatedAt")] [BsonIgnoreIfNull] public DateTime? EscalatedAt { get; set; }
}
