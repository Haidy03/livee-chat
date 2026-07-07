using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Core.ValueObjects;

[BsonIgnoreExtraElements]
public sealed class FlowNode
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("position")]
    public NodePosition Position { get; set; } = new();

    [BsonElement("data")]
    public NodeData Data { get; set; } = new();
}

[BsonIgnoreExtraElements]
public sealed class NodePosition
{
    [BsonElement("x")]
    public double X { get; set; }

    [BsonElement("y")]
    public double Y { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class NodeData
{
    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    [BsonElement("config")]
    public BsonDocument? Config { get; set; }
}
