using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CtiBackend.Models.HubSpot;

public sealed class HubSpotIntegration
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("hubSpotAccountId")]
    public string? HubSpotAccountId { get; set; }

    [BsonElement("hubSpotAccountName")]
    public string? HubSpotAccountName { get; set; }

    [BsonElement("encryptedAccessToken")]
    public string? EncryptedAccessToken { get; set; }

    [BsonElement("encryptedRefreshToken")]
    public string? EncryptedRefreshToken { get; set; }

    [BsonElement("accessTokenExpiresAtUtc")]
    public DateTime? AccessTokenExpiresAtUtc { get; set; }

    [BsonElement("grantedScopes")]
    public List<string> GrantedScopes { get; set; } = [];

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public HubSpotIntegrationStatus Status { get; set; }

    [BsonElement("connectedByUserId")]
    public string? ConnectedByUserId { get; set; }

    [BsonElement("connectedAtUtc")]
    public DateTime? ConnectedAtUtc { get; set; }

    [BsonElement("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }

    [BsonElement("lastRefreshedAtUtc")]
    public DateTime? LastRefreshedAtUtc { get; set; }

    [BsonElement("disconnectedAtUtc")]
    public DateTime? DisconnectedAtUtc { get; set; }

    [BsonElement("lastErrorCode")]
    public string? LastErrorCode { get; set; }

    [BsonElement("lastErrorAtUtc")]
    public DateTime? LastErrorAtUtc { get; set; }

    [BsonElement("version")]
    public long Version { get; set; }
}
