using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.FreeSwitchXmlCurl.Models;

[BsonIgnoreExtraElements]
public sealed class DialplanAction
{
    [BsonElement("application")]
    public string Application { get; set; } = default!;

    [BsonElement("data")]
    [BsonIgnoreIfNull]
    public string? Data { get; set; }
}
