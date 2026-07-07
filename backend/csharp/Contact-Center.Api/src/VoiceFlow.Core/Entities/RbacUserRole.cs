using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class RbacUserRole : Entity, ITenantScoped
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("roleId")]
    public string RoleId { get; set; } = string.Empty;

    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonIgnore]
    public new DateTime UpdatedAt { get; } = DateTime.MinValue;
}
