using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Core.Entities.Reports;

[BsonIgnoreExtraElements]
public sealed class Bi
{
    [BsonElement("en")]
    public string En { get; set; } = string.Empty;
    [BsonElement("ar")]
    public string Ar { get; set; } = string.Empty;
}
