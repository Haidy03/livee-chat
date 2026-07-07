using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CtiBackend.Models.HubSpot;

public sealed class HubSpotOAuthState
{
    [BsonId]
    public ObjectId Id { get; set; }
    public string StateHash { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ReturnPath { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
}
