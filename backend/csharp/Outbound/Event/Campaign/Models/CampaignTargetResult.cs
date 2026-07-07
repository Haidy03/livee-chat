using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Outbound.Event.Campaign.Models;

/// <summary>
/// One processed-target record written by the consumer to the <c>campaigntargetresulttest</c>
/// collection so outcomes are visible in MongoDB (companion to the log-file placeholder).
/// </summary>
[BsonIgnoreExtraElements]
public sealed class CampaignTargetResult
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("campaignId")]
    public string CampaignId { get; set; } = string.Empty;

    [BsonElement("targetId")]
    public string TargetId { get; set; } = string.Empty;

    [BsonElement("phone")]
    public string Phone { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("disposition")]
    public string? Disposition { get; set; }

    [BsonElement("attempts")]
    public int Attempts { get; set; }

    [BsonElement("correlationId")]
    public string CorrelationId { get; set; } = string.Empty;

    [BsonElement("processedAt")]
    public DateTime ProcessedAt { get; set; }
}
