using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Core.ValueObjects;

[BsonIgnoreExtraElements]
public sealed class FlowEdge
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("source")]
    public string Source { get; set; } = string.Empty;

    [BsonElement("sourceHandle")]
    public string SourceHandle { get; set; } = string.Empty;

    [BsonElement("target")]
    public string Target { get; set; } = string.Empty;

    [BsonElement("targetHandle")]
    public string TargetHandle { get; set; } = string.Empty;

    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    [BsonElement("tone")]
    public string Tone { get; set; } = string.Empty;
}
