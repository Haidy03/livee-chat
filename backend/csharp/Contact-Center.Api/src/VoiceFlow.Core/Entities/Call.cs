using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Enums;

namespace VoiceFlow.Core.Entities;

[BsonIgnoreExtraElements]
public sealed class Call : Entity, ITenantScoped
{
    [BsonElement("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("callId")]
    public string? CallId { get; set; }

    [BsonElement("direction")]
    public CallDirection Direction { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public CallStatus Status { get; set; }

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("answeredAt")]
    public DateTime? AnsweredAt { get; set; }

    [BsonElement("endedAt")]
    public DateTime? EndedAt { get; set; }

    [BsonElement("ringSeconds")]
    public int RingSeconds { get; set; }

    [BsonElement("holdSeconds")]
    public int HoldSeconds { get; set; }

    [BsonElement("totalHoldSeconds")]
    public int TotalHoldSeconds { get; set; }

    [BsonElement("activeSeconds")]
    public int ActiveSeconds { get; set; }

    [BsonElement("totalSeconds")]
    public int TotalSeconds { get; set; }

    [BsonElement("hangupCause")]
    public string? HangupCause { get; set; }


    //----------------------

    [BsonElement("callerId")]
    public string? CallerId { get; set; }

    [BsonElement("callerName")]
    public string? CallerName { get; set; }

    [BsonElement("callerExtension")]
    public string? CallerExtension { get; set; }

    [BsonElement("callerIsAiAgent")]
    public bool CallerIsAiAgent { get; set; }

    [BsonElement("calledId")]
    public string? CalledId { get; set; }

    [BsonElement("calledName")]
    public string? CalledName { get; set; }

    [BsonElement("calledExtension")]
    public string? CalledExtension { get; set; }

    [BsonElement("calledIsAiAgent")]
    public bool CalledIsAiAgent { get; set; }
    //---------------------



    [BsonElement("agentId")]
    public string? AgentId { get; set; }

    [BsonElement("groupId")]
    public string? GroupId { get; set; }

    [BsonElement("caller")]
    public string Caller { get; set; } = string.Empty;

    [BsonElement("called")]
    public string Called { get; set; } = string.Empty;

    [BsonElement("fromUri")]
    public string? FromUri { get; set; }

    [BsonElement("fromDisplay")]
    public string? FromDisplay { get; set; }

    [BsonElement("toUri")]
    public string? ToUri { get; set; }

    [BsonElement("toDisplay")]
    public string? ToDisplay { get; set; }

    [BsonElement("tagIds")]
    public List<string> TagIds { get; set; } = [];

    [BsonElement("autoTagIds")]
    public List<string> AutoTagIds { get; set; } = [];

    [BsonElement("sentiment")]
    public Sentiment? Sentiment { get; set; }

    [BsonElement("inputs")]
    public string Inputs { get; set; } = string.Empty;

    [BsonElement("hasRecording")]
    public bool HasRecording { get; set; }

    [BsonElement("recordingUrl")]
    public string? RecordingUrl { get; set; }

    [BsonElement("summary")]
    public string? Summary { get; set; }

    [BsonElement("summaryLanguage")]
    public string? SummaryLanguage { get; set; }

    [BsonElement("summaryAccuracyFeedback")]
    public string? SummaryAccuracyFeedback { get; set; }

    [BsonElement("fullTranscript")]
    public string? FullTranscript { get; set; }

    [BsonElement("segments")]
    public List<TranscriptSegment>? Segments { get; set; }

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty;

    [BsonElement("wrapUp")]
    public CallWrapUp? WrapUp { get; set; }
}


[BsonIgnoreExtraElements]
public sealed class TranscriptSegment
{
    [BsonElement("speaker")]
    public string Speaker { get; init; } = "Unknown";
    [BsonElement("text")]
    public string Text { get; init; } = string.Empty;
    [BsonElement("offset")]
    public TimeSpan Offset { get; init; }
    [BsonElement("duration")]
    public TimeSpan Duration { get; init; }
}


public sealed class CallWrapUp
{
    [BsonElement("disposition")]
    public string Disposition { get; set; } = string.Empty;

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("callbackScheduled")]
    public bool CallbackScheduled { get; set; }
    [BsonElement("acwSeconds")]
    public int AcwSeconds { get; set; }

    [BsonElement("completedAt")]
    public DateTime CompletedAt { get; set; }

    [BsonElement("agentId")]
    public string AgentId { get; set; } = string.Empty;

    [BsonElement("status")]
    public string Status { get; set; } = "wrapped";
}
