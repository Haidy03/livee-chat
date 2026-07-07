using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class RbacRole : Entity
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "active";

    [BsonElement("isSystem")]
    public bool IsSystem { get; set; }

    [BsonElement("permissions")]
    public Dictionary<string, List<string>> Permissions { get; set; } = [];
}
