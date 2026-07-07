using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Logging;

public sealed class AgentHubLogEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "";
    public string Category { get; set; } = "";
    public int EventId { get; set; }
    public string? EventName { get; set; }
    public string Message { get; set; } = "";
    public string? Template { get; set; }
    public Dictionary<string, string?> Properties { get; set; } = new();
    public string? Exception { get; set; }
    public string MachineName { get; set; } = Environment.MachineName;
}
