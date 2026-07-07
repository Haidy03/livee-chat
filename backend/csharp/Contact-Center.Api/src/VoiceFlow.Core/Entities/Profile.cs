using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class Profile : Entity, ITenantScoped
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("email")]
    public string? Email { get; set; }

    [BsonElement("firstName")]
    public string? FirstName { get; set; }

    [BsonElement("lastName")]
    public string? LastName { get; set; }

    [BsonElement("displayName")]
    public string? DisplayName { get; set; }

    [BsonElement("extensionNumber")]
    public int? ExtensionNumber { get; set; }

    [BsonElement("outboundCid")]
    public string? OutboundCid { get; set; }

    [BsonElement("role")]
    public string Role { get; set; } = "agent";

    [BsonElement("status")]
    public string Status { get; set; } = "active";

    [BsonElement("disabled")]
    public bool Disabled { get; set; }

    [BsonElement("language")]
    public string Language { get; set; } = "en";

    [BsonElement("timezone")]
    public string Timezone { get; set; } = "UTC";

    [BsonElement("browserNotifications")]
    public bool BrowserNotifications { get; set; } = true;

    [BsonElement("groups")]
    public List<string> Groups { get; set; } = [];

    // Recording preferences

    [BsonElement("recordInboundInternal")]
    public bool RecordInboundInternal { get; set; }

    [BsonElement("recordInboundExternal")]
    public bool RecordInboundExternal { get; set; }

    [BsonElement("recordOutboundInternal")]
    public bool RecordOutboundInternal { get; set; }

    [BsonElement("recordOutboundExternal")]
    public bool RecordOutboundExternal { get; set; }

    [BsonElement("recordOnDemand")]
    public bool RecordOnDemand { get; set; }

    [BsonElement("skills")]
    public List<ProfileSkill> Skills { get; set; } = [];

    [BsonElement("availableChannels")]
    public List<string> AvailableChannels { get; set; } = new() { "voice" };

    [BsonIgnore]
    public string FullName =>
        !string.IsNullOrWhiteSpace(DisplayName)
            ? DisplayName
            : string.Join(' ',
                new[] { FirstName, LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));


    
}

[BsonIgnoreExtraElements]
public sealed class ProfileSkill
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("category")]
    public string Category { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("proficiency")]
    public int Proficiency { get; set; }

    [BsonElement("priority")]
    public int Priority { get; set; }

    [BsonElement("mandatory")]
    public bool Mandatory { get; set; }

    [BsonElement("active")]
    public bool Active { get; set; }
}
