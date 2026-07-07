using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class Contact : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("company")]
    public string Company { get; set; } = string.Empty;

    [BsonElement("tagIds")]
    public List<string> TagIds { get; set; } = [];

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty;

    [BsonElement("lastCallAt")]
    public DateTime? LastCallAt { get; set; }

    [BsonElement("totalCalls")]
    public int TotalCalls { get; set; }
}
