using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities.SkillCatalog;

[BsonIgnoreExtraElements]
public sealed class SkillCategory : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("active")]
    public bool Active { get; set; } = true;

    [BsonElement("options")]
    public List<SkillOption> Options { get; set; } = [];

}

[BsonIgnoreExtraElements]
public sealed class SkillOption
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("active")]
    public bool Active { get; set; } = true;

    [BsonElement("usageCount")]
    public int UsageCount { get; set; }
}
