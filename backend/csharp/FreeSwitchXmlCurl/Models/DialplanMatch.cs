using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.FreeSwitchXmlCurl.Models;

[BsonIgnoreExtraElements]
public sealed class DialplanMatch
{
    [BsonElement("field")]
    public string Field { get; set; } = "destination_number";

    /// <summary>exact | prefix | regex</summary>
    [BsonElement("type")]
    public string Type { get; set; } = "regex";

    [BsonElement("pattern")]
    [BsonIgnoreIfNull]
    public string? Pattern { get; set; }

    [BsonElement("value")]
    [BsonIgnoreIfNull]
    public string? Value { get; set; }
}
