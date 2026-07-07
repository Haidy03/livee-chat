using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class SipAccount : Entity, ITenantScoped
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("sipUri")]
    public string SipUri { get; set; } = string.Empty;

    [BsonElement("authId")]
    public string AuthId { get; set; } = string.Empty;

    [BsonElement("wsUrl")]
    public string WsUrl { get; set; } = string.Empty;

    [BsonElement("stunUrls")]
    public List<string> StunUrls { get; set; } = ["stun:stun.l.google.com:19302"];

    [BsonElement("turnUrl")]
    public string TurnUrl { get; set; } = string.Empty;

    [BsonElement("turnUsername")]
    public string TurnUsername { get; set; } = string.Empty;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("ppxPassword")]
    public string PPXPassword { get; set; } = "Soft@123";
}
