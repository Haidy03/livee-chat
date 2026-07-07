using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities.HubSpot;

[BsonIgnoreExtraElements]
public sealed class HubSpotOAuthState : Entity
{
    [BsonElement("stateHash")]
    public string StateHash { get; set; } = string.Empty;

    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("returnPath")]
    public string? ReturnPath { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; }

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("usedAtUtc")]
    public DateTime? UsedAtUtc { get; set; }
}
