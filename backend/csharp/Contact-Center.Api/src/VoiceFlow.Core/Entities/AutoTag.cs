using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class AutoTag : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [BsonElement("color")]
    public string Color { get; set; } = "#3B82F6";

    [BsonElement("tagId")]
    [BsonIgnoreIfNull]
    public string? TagId { get; set; }

    [BsonElement("active")]
    public bool Active { get; set; } = true;
}
