using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Api.LiveChat.Domain;

[BsonIgnoreExtraElements]
public class CannedResponse
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; } = string.Empty;

    public string projectId { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty; // each title has a list of messages to choose one from
    public List<string> messages { get; set; } = new();
    public string createdBy { get; set; } = string.Empty;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;
    public DateTime createdAt { get; set; } = DateTime.UtcNow;

    public CannedResponse() { }

    public CannedResponse(string projectId, string createdBy, string title)
    {
        this.projectId = projectId;
        this.createdBy = createdBy;
        this.title = title;
        this.updatedAt = DateTime.UtcNow;
        this.createdAt = DateTime.UtcNow;
        this.messages = new List<string>();
    }
}
