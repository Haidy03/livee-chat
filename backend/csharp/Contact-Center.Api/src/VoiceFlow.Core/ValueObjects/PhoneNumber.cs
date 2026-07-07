using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Core.ValueObjects;

[BsonIgnoreExtraElements]
public sealed record PhoneNumber(
    [property: BsonElement("number")] string Number,
    [property: BsonElement("label")] string Label = "");
