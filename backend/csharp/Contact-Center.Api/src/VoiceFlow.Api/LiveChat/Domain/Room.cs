using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace VoiceFlow.Api.LiveChat.Domain;

// Field casing matches existing MongoDB documents exactly — do NOT rename.
[BsonIgnoreExtraElements]
public class Room
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string agentId { get; set; } = string.Empty;
    public string agentName { get; set; } = string.Empty;
    public string agentConnectionId { get; set; } = string.Empty;
    public string agentChannel { get; set; } = "liveChat";
    public ConnectionStatus agentStatus { get; set; } = new();

    public string clientId { get; set; } = string.Empty;
    public string clientConnectionId { get; set; } = string.Empty;
    public string contactId { get; set; } = string.Empty;
    public string lang { get; set; } = string.Empty;

    public string roomStatus { get; set; } = "pending"; // pending, active
    public bool bicRoom { get; set; }
    public bool lead { get; set; }
    public string perviousBicRoomId { get; set; } = string.Empty;
    public string funnelId { get; set; } = string.Empty;
    public string funnelStageId { get; set; } = string.Empty;

    //changed by AT
    public string channel { get; set; } = string.Empty;
    public string clientMessage { get; set; } = string.Empty;
    public string clientInfo { get; set; } = string.Empty;
    public ConnectionStatus clientStatus { get; set; } = new();
    public string projectId { get; set; } = string.Empty;
    public string chatbotId { get; set; } = string.Empty;
    public int conversationId { get; set; }
    public DepartmentInfo department { get; set; } = new();

    public DateTime created { get; set; } = DateTime.UtcNow;
    public DateTime updatedAt { get; set; } = DateTime.UtcNow;
    public DateTime lastAgentMessage { get; set; }
    public DateTime? lastClientMessage { get; set; }
    public int period { get; set; }

    //ahmed taha
    public int firstResponseTime { get; set; } = -1;
    public int responseCount { get; set; }
    public int cause { get; set; } = 0; //0 normal 1-forced 2-agent not responding 3-agent offline 4-user 5-user-offline 6-agent transfer 7-client not responding 8-client reject

    //room History
    public int rate { get; set; } = -1;
    public string comment { get; set; } = string.Empty;
    public int templateMessageCount { get; set; }
    public DateTime lastTemplateMessage { get; set; }

    //tags
    public List<RoomTag> tags { get; set; } = new();
    public List<RoomNote> notes { get; set; } = new();

  
    public string source { get; set; } = string.Empty; //when agent asks to connect with client from request/history/contacts

    // --- Extras (not in legacy schema, kept for current app behavior) ---
    public string typeOfClose { get; set; } = string.Empty;
    public List<Message> Messages { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class RoomNote
{
    public string text { get; set; } = string.Empty;
    public string createdBy { get; set; } = string.Empty;
    public DateTime createdAt { get; set; } = DateTime.UtcNow;
}

[BsonIgnoreExtraElements]
public class RoomTag
{
    [Required]
    [BsonRepresentation(BsonType.ObjectId)]
    public string _id { get; set; } = ObjectId.GenerateNewId().ToString();

    [Required]
    public string name { get; set; } = string.Empty;

    [Required]
    public string color { get; set; } = string.Empty;
}

[BsonIgnoreExtraElements]
public class Message
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public string RoomId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public MessageDirection Direction { get; set; }

    [BsonRepresentation(BsonType.String)]
    public SenderType SenderType { get; set; }

    public string SenderId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool DeliveredToChannel { get; set; }
    public List<MessageAttachment> Attachments { get; set; } = new();
}

[BsonIgnoreExtraElements]
public class MessageAttachment
{
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
    public string Kind { get; set; } = "document";
    public string Url { get; set; } = string.Empty;
    public string? Name { get; set; }
    public long? SizeBytes { get; set; }
    public string? Mime { get; set; }
    public int? DurationSec { get; set; }
}

/// <summary>
/// A conversation is the ordered list of messages that belong to a <see cref="Room"/>.
/// It is not persisted on its own — it is a typed view over <see cref="Room.Messages"/>.
/// </summary>
public sealed class Conversation : List<Message>
{
    public Conversation() { }
    public Conversation(IEnumerable<Message> source) : base(source) { }
}
