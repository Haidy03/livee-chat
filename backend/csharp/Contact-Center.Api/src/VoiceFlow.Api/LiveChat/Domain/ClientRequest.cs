using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Api.LiveChat.Domain;

// Field casing matches existing MongoDB documents exactly — do NOT rename.
[BsonIgnoreExtraElements]
public class ClientRequest
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string userId { get; set; } = string.Empty;
    public string channel { get; set; } = string.Empty;
    public string agentChannel { get; set; } = string.Empty;
    public string projectId { get; set; } = string.Empty;
    public string chatbotId { get; set; } = string.Empty;
    public string contact_Id { get; set; } = string.Empty;
    public DepartmentInfo department { get; set; } = new();
    public string lang { get; set; } = string.Empty;
    public string connectionId { get; set; } = string.Empty;
    public ConnectionStatus status { get; set; } = new();
    public DateTime created { get; set; } = DateTime.UtcNow;
    // Intentional misspelling — matches existing production documents.
    public DateTime upadtedAt { get; set; } = DateTime.UtcNow;
    public string execludedAgentId { get; set; } = string.Empty;
    public string clientInfo { get; set; } = string.Empty;
    public string typeOfClose { get; set; } = string.Empty;
    public bool locked { get; set; }
    public string comment { get; set; } = string.Empty;
    public int requestCount { get; set; }
}

[BsonIgnoreExtraElements]
public class ConnectionStatus
{
    public string state { get; set; } = "online";
    public DateTime timeStamp { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class DepartmentInfo
{
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonIgnoreIfNull]
    public string id { get; set; } = null!;
    public string name { get; set; } = string.Empty;
}
