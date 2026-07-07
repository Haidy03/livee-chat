using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Core.ValueObjects;

public sealed class UnbilledBreakdownItem
{
    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;

    [BsonElement("amount")]
    public decimal Amount { get; set; }
}
