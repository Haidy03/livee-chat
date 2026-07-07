using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Api.LiveChat.Domain;

[BsonIgnoreExtraElements]
public class Agent
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string DisplayName { get; set; } = string.Empty;
    public List<string> DepartmentIds { get; set; } = new();
    public List<string> Languages { get; set; } = new();
    public int MaxConcurrency { get; set; } = 4;
    public bool VoiceEnabled { get; set; }
}
