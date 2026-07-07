// ============================================================================
// COPIED VERBATIM from VoiceFlow.Reports (UsersMapIngestController.State and
// its supporting types). DO NOT CHANGE the logic, models, property names, or
// behavior. The only adjustments are namespace/visibility wrappers needed to
// compile inside CtiBackend. Original sources:
//   - backend/csharp/ReportsService/.../UsersMapIngestController.cs
//   - backend/csharp/ReportsService/.../Telemetry/CallState.cs
//   - backend/csharp/ReportsService/.../Telemetry/LiveCall.cs
//   - backend/csharp/ReportsService/.../Telemetry/ILiveCallRegistry.cs
//   - backend/csharp/ReportsService/.../Entities/CallRecord.cs
//   - backend/csharp/ReportsService/.../Abstractions/ICallRecordRepository.cs
//   - backend/csharp/ReportsService/.../Application/Telemetry/NodeKindMap.cs
//   - backend/csharp/ReportsService/.../Contracts/Requests/UsersMapIngestRequests.cs
//   - backend/csharp/ReportsService/.../Contracts/Common/ApiResponse.cs
// ============================================================================

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CtiBackend.Services.State.UsersMap;

// ---- CallState (verbatim) --------------------------------------------------

[JsonConverter(typeof(CallStateJsonConverter))]
public enum CallState
{
    CallStart,
    WorkingHours,
    IvrMenu,
    SoundFile,
    UserInput,
    Messages,
    RuleRouting,
    SetFlags,
    Webhook,
    CallForwarding,
    AiAgent,
    Voicemail,
    Survey,
    EndCall,
    Queue,
}

public static class CallStateExtensions
{
    private static readonly Dictionary<CallState, string> Wire = new()
    {
        [CallState.CallStart]      = "call_start",
        [CallState.WorkingHours]   = "working_hours",
        [CallState.IvrMenu]        = "ivr_menu",
        [CallState.SoundFile]      = "sound_file",
        [CallState.UserInput]      = "user_input",
        [CallState.Messages]       = "messages",
        [CallState.RuleRouting]    = "rule_routing",
        [CallState.SetFlags]       = "set_flags",
        [CallState.Webhook]        = "webhook",
        [CallState.CallForwarding] = "call_forwarding",
        [CallState.AiAgent]        = "ai_agent",
        [CallState.Voicemail]      = "voicemail",
        [CallState.Survey]         = "survey",
        [CallState.EndCall]        = "end_call",
        [CallState.Queue]          = "queue",
    };

    private static readonly Dictionary<string, CallState> ByWire =
        Wire.ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, CallState> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ivr"]      = CallState.IvrMenu,
        ["menu"]     = CallState.IvrMenu,
        ["hours"]    = CallState.WorkingHours,
        ["ai"]       = CallState.AiAgent,
        ["agent"]    = CallState.CallForwarding,
        ["transfer"] = CallState.CallForwarding,
        ["vm"]       = CallState.Voicemail,
        ["queue_main"] = CallState.Queue,
    };

    public static string ToWire(this CallState s) => Wire[s];

    public static bool TryParseWire(string? value, out CallState state)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (ByWire.TryGetValue(value!, out state)) return true;
            if (Aliases.TryGetValue(value!, out state)) return true;
        }
        state = CallState.CallStart;
        return false;
    }

    public static CallState ParseWireOrDefault(string? value, CallState fallback = CallState.CallStart)
        => TryParseWire(value, out var s) ? s : fallback;

    // ---- coarse-bucket helpers (verbatim from ReportsService CallState.cs) ----

    public static bool IsIvrFamily(this CallState s) => s switch
    {
        CallState.CallStart or CallState.WorkingHours or CallState.IvrMenu or
        CallState.SoundFile or CallState.UserInput or CallState.Messages or
        CallState.RuleRouting or CallState.SetFlags or CallState.Webhook => true,
        _ => false,
    };

    public static bool IsAi(this CallState s) => s == CallState.AiAgent;
    public static bool IsAgent(this CallState s) => s == CallState.CallForwarding;
    public static bool IsVoicemail(this CallState s) => s == CallState.Voicemail;
    public static bool IsSurvey(this CallState s) => s == CallState.Survey;
    public static bool IsQueue(this CallState s) => s == CallState.Queue;

    public static string ToCoarseBucket(this CallState s) => s switch
    {
        var x when x.IsIvrFamily() => "ivr",
        CallState.AiAgent          => "ai",
        CallState.CallForwarding   => "agent",
        CallState.Queue            => "queue",
        CallState.Voicemail        => "vm",
        CallState.Survey           => "survey",
        CallState.EndCall          => "end",
        _ => "ivr",
    };

    public static IEnumerable<CallState> All => Wire.Keys;
}

internal sealed class CallStateJsonConverter : JsonConverter<CallState>
{
    public override CallState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        return CallStateExtensions.ParseWireOrDefault(s);
    }
    public override void Write(Utf8JsonWriter writer, CallState value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToWire());
}

// ---- NodeKindMap (verbatim) ------------------------------------------------

public static class NodeKindMap
{
    private static readonly Dictionary<string, CallState> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["call_start"]      = CallState.CallStart,
        ["working_hours"]   = CallState.WorkingHours,
        ["hours"]           = CallState.WorkingHours,
        ["ivr_menu"]        = CallState.IvrMenu,
        ["menu"]            = CallState.IvrMenu,
        ["sound_file"]      = CallState.SoundFile,
        ["user_input"]      = CallState.UserInput,
        ["messages"]        = CallState.Messages,
        ["rule_routing"]    = CallState.RuleRouting,
        ["set_flags"]       = CallState.SetFlags,
        ["webhook"]         = CallState.Webhook,
        ["call_forwarding"] = CallState.CallForwarding,
        ["transfer"]        = CallState.CallForwarding,
        ["agent"]           = CallState.CallForwarding,
        ["ai_agent"]        = CallState.AiAgent,
        ["ai"]              = CallState.AiAgent,
        ["voicemail"]       = CallState.Voicemail,
        ["vm"]               = CallState.Voicemail,
        ["survey"]          = CallState.Survey,
        ["end_call"]        = CallState.EndCall,
        ["queue"]           = CallState.Queue,
        ["queue_main"]      = CallState.Queue,
    };

    private static readonly ConcurrentDictionary<string, byte> WarnedKinds = new();

    public static CallState ToState(string? kind, ILogger? logger = null)
    {
        if (string.IsNullOrWhiteSpace(kind)) return CallState.CallStart;
        if (Map.TryGetValue(kind, out var state)) return state;
        if (logger != null && WarnedKinds.TryAdd(kind, 0))
            logger.LogWarning("Unknown flow node kind '{Kind}' — defaulting to 'call_start'", kind);
        return CallState.CallStart;
    }
}

// ---- LiveCall + friends (verbatim) -----------------------------------------

public sealed class LiveCall
{
    public string Id { get; set; } = string.Empty;
    public string CallId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MaskedNumber { get; set; } = string.Empty;
    public string Color { get; set; } = "#647590";
    public CallState State { get; set; } = CallState.CallStart;
    public string EnteredStateAt { get; set; } = string.Empty;
    public string CallStartedAt { get; set; } = string.Empty;
    public string? FlowId { get; set; }
    public string? NodeKey { get; set; }
    public string? NodeKind { get; set; }
    public string? NodeLabel { get; set; }
    public string? IvrChoice { get; set; }
    public string? Intent { get; set; }
    public string? Detail { get; set; }
    public LiveAgentRef? Agent { get; set; }
    public int? QueuePosition { get; set; }
    public int? SurveyStep { get; set; }
    public int? SurveyTotal { get; set; }
    public string Channel { get; set; } = "voice";
    public string Direction { get; set; } = "inbound";
    public List<string>? Tags { get; set; }
    public List<LiveJourneyStep> History { get; set; } = new();
}

public sealed class LiveAgentRef
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;
}

public sealed class LiveJourneyStep
{
    [BsonElement("at")]
    public string At { get; set; } = string.Empty;
    [BsonElement("nodeKey")]
    public string NodeKey { get; set; } = string.Empty;
    [BsonElement("kind")]
    public string? Kind { get; set; }
    [BsonElement("label")]
    public string Label { get; set; } = string.Empty;
    [BsonElement("sub")]
    public string? Sub { get; set; }
}

public sealed class LiveCallsSnapshot
{
    public IReadOnlyList<LiveCall> Calls { get; init; } = Array.Empty<LiveCall>();
    public IReadOnlyDictionary<string, int> StateCounts { get; init; } = new Dictionary<string, int>();
    public IReadOnlyDictionary<string, int> NodeCounts { get; init; } = new Dictionary<string, int>();
    public IReadOnlyList<string> QueueOrder { get; init; } = Array.Empty<string>();
    public int LongestWaitSec { get; init; }
    public int AvgHandleSec { get; init; }
    public int SlaPercent { get; init; }
    public int SlaTargetPercent { get; init; }
    public int AbandonedToday { get; init; }
}

// ---- CallRecord (verbatim) -------------------------------------------------

[BsonIgnoreExtraElements]
public sealed class CallRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

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

    [BsonElement("finalState")]
    public string FinalState { get; set; } = "end_call";

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

// ---- Ingest DTOs (verbatim) ------------------------------------------------

public sealed class IngestStateRequest
{
    public string TenantId { get; set; } = "default";
    public string CallId { get; set; } = string.Empty;
    public IngestNode Node { get; set; } = new();
    public string? State { get; set; }
    public IngestCaller? Caller { get; set; }
    public string? IvrChoice { get; set; }
    public string? Intent { get; set; }
    public IngestAgentRef? Agent { get; set; }
    public int? QueuePosition { get; set; }
    public IngestSurvey? Survey { get; set; }
    public Dictionary<string, object>? Flags { get; set; }
    public IngestWebhook? Webhook { get; set; }
    public List<string>? Tags { get; set; }
    public string? At { get; set; }
    public string? Channel { get; set; }
    public string? Direction { get; set; }
    public string? Color { get; set; }
}

public sealed class IngestNode
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string? FlowId { get; set; }
}

public sealed class IngestCaller
{
    public string? Name { get; set; }
    public string? Number { get; set; }
}

public sealed class IngestAgentRef
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public sealed class IngestSurvey
{
    public int Step { get; set; }
    public int Total { get; set; }
}

public sealed class IngestWebhook
{
    public string? Name { get; set; }
}

// ---- ApiResponse (verbatim — local UsersMap copy) --------------------------

public sealed class UsersMapApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static UsersMapApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };
}

// ---- Abstractions (verbatim) -----------------------------------------------

public interface ILiveCallRegistry
{
    Task RecordStateAsync(LiveCall call, CancellationToken ct);
    Task RemoveAsync(string tenantId, string callId, string reason, CancellationToken ct);
    Task UpdateMetricsAsync(string tenantId, int? avgHandleSec, int? slaPercent, int? slaTargetPercent, CancellationToken ct);
    Task<LiveCallsSnapshot> GetSnapshotAsync(string tenantId, CancellationToken ct);
}

public interface ICallRecordRepository
{
    Task AddAsync(CallRecord record, CancellationToken ct);
}
