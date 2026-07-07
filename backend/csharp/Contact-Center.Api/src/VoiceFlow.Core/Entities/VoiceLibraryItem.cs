using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class VoiceLibraryItem : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("source")]
    public string Source { get; set; } = "upload";

    [BsonElement("text")]
    public string? Text { get; set; }

    [BsonElement("filePath")]
    public string? FilePath { get; set; }

    [BsonElement("url")]
    public string? Url { get; set; }

    [BsonElement("language")]
    public string Language { get; set; } = "ar";

    [BsonElement("voice")]
    public string Voice { get; set; } = "female";

    [BsonElement("interruptible")]
    public bool Interruptible { get; set; } = true;

    [BsonElement("duration")]
    public int? Duration { get; set; }
}
