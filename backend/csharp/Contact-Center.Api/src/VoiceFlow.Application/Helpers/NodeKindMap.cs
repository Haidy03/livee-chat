using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using VoiceFlow.Core.Helpers;

namespace VoiceFlow.Application.Helpers;

/// <summary>
/// Maps a flow-editor node kind (see src/features/flow-editor/types.ts) to a
/// <see cref="CallState"/>. There is one-to-one correspondence for every
/// known kind; a handful of legacy/alias names are also accepted.
/// </summary>
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
        ["vm"]              = CallState.Voicemail,
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

    public static bool IsKnown(string? kind) =>
        !string.IsNullOrWhiteSpace(kind) && Map.ContainsKey(kind);
}
