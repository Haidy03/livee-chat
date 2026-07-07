using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class SoftphoneCallLog : Entity, ITenantScoped
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("direction")]
    public string Direction { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("number")]
    public string Number { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("contactId")]
    public string? ContactId { get; set; }

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("durationSec")]
    public int DurationSec { get; set; }

    [BsonElement("failureReason")]
    public string FailureReason { get; set; } = string.Empty;

    [BsonIgnore]
    public new DateTime UpdatedAt { get; } = DateTime.MinValue;
}
