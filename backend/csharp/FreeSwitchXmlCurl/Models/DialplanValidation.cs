using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.FreeSwitchXmlCurl.Models;

[BsonIgnoreExtraElements]
public sealed class DialplanValidation
{
    [BsonElement("validateExtensionExists")]
    [BsonIgnoreIfNull]
    public bool? ValidateExtensionExists { get; set; }

    [BsonElement("validateDidExists")]
    [BsonIgnoreIfNull]
    public bool? ValidateDidExists { get; set; }

    [BsonElement("validateGatewayExists")]
    [BsonIgnoreIfNull]
    public bool? ValidateGatewayExists { get; set; }

    [BsonElement("validateIvrExists")]
    [BsonIgnoreIfNull]
    public bool? ValidateIvrExists { get; set; }
}
