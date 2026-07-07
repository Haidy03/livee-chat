using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoiceFlow.Core.Helpers;

/// <summary>
/// Per-node-kind state for a live caller. Wire format is the snake_case
/// label (e.g. "ivr_menu") so it matches the flow-editor NodeKind union and
/// the dialplan save-status payload byte-for-byte. <see cref="Queue"/> is
/// synthetic — no flow node maps to it, it's set when a call is parked in
/// an ACD queue by call_forwarding.
/// </summary>
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

    /// <summary>Legacy coarse-bucket aliases accepted on the wire.</summary>
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

    // ---- coarse-bucket helpers ----

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

    /// <summary>Coarse bucket label used by the Users Map UI (ivr|ai|agent|queue|vm|survey).</summary>
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
