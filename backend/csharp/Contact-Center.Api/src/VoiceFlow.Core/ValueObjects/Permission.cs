using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Core.ValueObjects;

[BsonIgnoreExtraElements]
public sealed record Permission(
    [property: BsonElement("module")] string Module,
    [property: BsonElement("action")] string Action);
