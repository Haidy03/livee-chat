using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class EditLog : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [BsonElement("entityId")]
    public string EntityId { get; set; } = string.Empty;

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    [BsonElement("field")]
    public string? Field { get; set; }

    [BsonElement("oldValue")]
    public BsonValue? OldValue { get; set; }

    [BsonElement("newValue")]
    public BsonValue? NewValue { get; set; }

    [BsonElement("summary")]
    public string? Summary { get; set; }

    [BsonElement("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = [];

    [BsonIgnore]
    public new DateTime UpdatedAt { get; } = DateTime.MinValue;
}
