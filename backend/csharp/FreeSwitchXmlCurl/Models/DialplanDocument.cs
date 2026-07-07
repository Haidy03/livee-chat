using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.FreeSwitchXmlCurl.Models;

[BsonIgnoreExtraElements]
public sealed class DialplanDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    [BsonElement("_id")]
    public string Id { get; set; } = default!;

    [BsonElement("tenantId")]
    [BsonIgnoreIfNull]
    public string? TenantId { get; set; }

    [BsonElement("domain")]
    public string Domain { get; set; } = default!;

    [BsonElement("context")]
    public string Context { get; set; } = default!;

    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("enabled")]
    public bool Enabled { get; set; } = true;

    [BsonElement("priority")]
    public int Priority { get; set; } = 100;

    [BsonElement("renderMode")]
    public string? RenderMode { get; set; } = "structured";

    [BsonElement("entries")]
    public List<DialplanEntry> Entries { get; set; } = new();
}
