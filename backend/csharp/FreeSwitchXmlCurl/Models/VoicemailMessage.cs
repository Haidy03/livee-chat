using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.FreeSwitchXmlCurl.Models;

[BsonIgnoreExtraElements]
public sealed class VoicemailMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("tenantId")] [BsonIgnoreIfNull] public string? TenantId { get; set; }
    [BsonElement("flowId")] [BsonIgnoreIfNull] public string? FlowId { get; set; }
    [BsonElement("nodeId")] [BsonIgnoreIfNull] public string? NodeId { get; set; }

    [BsonElement("mailbox")] public string Mailbox { get; set; } = default!;
    [BsonElement("domain")] public string Domain { get; set; } = default!;
    [BsonElement("context")] [BsonIgnoreIfNull] public string? Context { get; set; }

    [BsonElement("uuid")] public string Uuid { get; set; } = default!;
    [BsonElement("callerIdNumber")] [BsonIgnoreIfNull] public string? CallerIdNumber { get; set; }
    [BsonElement("destinationNumber")] [BsonIgnoreIfNull] public string? DestinationNumber { get; set; }

    [BsonElement("recordingPath")] [BsonIgnoreIfNull] public string? RecordingPath { get; set; }
    [BsonElement("fileSize")] [BsonIgnoreIfNull] public long? FileSize { get; set; }
    [BsonElement("format")] public string Format { get; set; } = "wav";
    [BsonElement("durationSeconds")] public int DurationSeconds { get; set; }

    [BsonElement("vmMessageExt")] [BsonIgnoreIfNull] public string? VmMessageExt { get; set; }

    [BsonElement("timestamp")] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
