using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Core.Entities;

/// <summary>Per-agent email channel preferences (currently just the signature).</summary>
[BsonIgnoreExtraElements]
public sealed class EmailAgentSettings : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("agentId")]
    public string AgentId { get; set; } = string.Empty;

    /// <summary>HTML signature appended below replies/composes in the editor.</summary>
    [BsonElement("signatureHtml")]
    public string SignatureHtml { get; set; } = string.Empty;
}
