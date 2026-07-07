using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities.WrapUpCodes;

[BsonIgnoreExtraElements]
public sealed class WrapUpCode : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;


    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    [BsonElement("labelAr")]
    public string? LabelAr { get; set; }

    [BsonElement("category")]
    public string Category { get; set; } = "general";

    [BsonElement("color")]
    public string Color { get; set; } = "#64748b";

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class QueueWrapUpCode : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("queueId")]
    public string QueueId { get; set; } = string.Empty;

    [BsonElement("wrapUpCodeId")]
    public string WrapUpCodeId { get; set; } = string.Empty;
}
