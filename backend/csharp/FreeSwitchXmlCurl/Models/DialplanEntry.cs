using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.FreeSwitchXmlCurl.Models;

[BsonIgnoreExtraElements]
public sealed class DialplanEntry
{
    [BsonElement("name")]
    public string Name { get; set; } = default!;

    [BsonElement("routeType")]
    public string? RouteType { get; set; }

    [BsonElement("priority")]
    public int Priority { get; set; } = 100;

    [BsonElement("match")]
    public DialplanMatch Match { get; set; } = new();

    [BsonElement("validation")]
    [BsonIgnoreIfNull]
    public DialplanValidation? Validation { get; set; }

    [BsonElement("actions")]
    public List<DialplanAction> Actions { get; set; } = new();
}
