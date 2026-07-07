using MongoDB.Bson.Serialization.Attributes;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Models;

namespace VoiceFlow.Reports.Core.Entities;

/// <summary>
/// Finalized call document persisted to MongoDB when a call reaches
/// <see cref="CallState.EndCall"/> (or is otherwise terminated). Mirrors the
/// in-flight <see cref="Telemetry.LiveCallRecord"/> with end-of-call fields added
/// (<see cref="EndedAt"/>, <see cref="DurationSec"/>, <see cref="Reason"/>).
/// </summary>
[BsonIgnoreExtraElements]
public sealed class LiveCall : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("callId")]
    public string CallId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("maskedNumber")]
    public string MaskedNumber { get; set; } = string.Empty;

    [BsonElement("color")]
    public string Color { get; set; } = "#647590";

    /// <summary>
    /// Wire label of the last CallState the call was in.
    /// </summary>
    [BsonElement("finalState")]
    public string FinalState { get; set; } = "end_call";

    /// <summary>
    /// completed | abandoned | failed
    /// </summary>
    [BsonElement("reason")]
    public string Reason { get; set; } = "completed";

    [BsonElement("callStartedAt")]
    public string CallStartedAt { get; set; } = string.Empty;

    [BsonElement("endedAt")]
    public string EndedAt { get; set; } = string.Empty;

    [BsonElement("durationSec")]
    public int DurationSec { get; set; }

    [BsonElement("flowId")]
    public string? FlowId { get; set; }

    [BsonElement("nodeKey")]
    public string? NodeKey { get; set; }

    [BsonElement("nodeKind")]
    public string? NodeKind { get; set; }

    [BsonElement("nodeLabel")]
    public string? NodeLabel { get; set; }

    [BsonElement("ivrChoice")]
    public string? IvrChoice { get; set; }

    [BsonElement("intent")]
    public string? Intent { get; set; }

    [BsonElement("detail")]
    public string? Detail { get; set; }

    [BsonElement("agent")]
    public LiveAgentRef? Agent { get; set; }

    [BsonElement("queuePosition")]
    public int? QueuePosition { get; set; }

    [BsonElement("surveyStep")]
    public int? SurveyStep { get; set; }

    [BsonElement("surveyTotal")]
    public int? SurveyTotal { get; set; }

    [BsonElement("channel")]
    public string Channel { get; set; } = "voice";

    [BsonElement("direction")]
    public string Direction { get; set; } = "inbound";

    [BsonElement("tags")]
    public List<string>? Tags { get; set; }

    [BsonElement("history")]
    public List<LiveJourneyStep> History { get; set; } = new();
}
