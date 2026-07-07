using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class Group : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("members")]
    public List<string> Members { get; set; } = [];

    [BsonElement("ringStrategy")]
    public RingStrategy RingStrategy { get; set; } = RingStrategy.Simultaneous;

    [BsonElement("ringTimeout")]
    public int RingTimeout { get; set; } = 30;

    [BsonElement("activeCalls")]
    public int ActiveCalls { get; set; } = 0;
}
