using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Enums;
using VoiceFlow.Core.ValueObjects;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class Flow : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("status")]
    public FlowStatus Status { get; set; } = FlowStatus.Draft;

    [BsonElement("assignedExtension")]
    public string? AssignedExtension { get; set; }

    [BsonElement("nodes")]
    public List<FlowNode> Nodes { get; set; } = [];

    [BsonElement("edges")]
    public List<FlowEdge> Edges { get; set; } = [];
}
