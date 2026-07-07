using CtiBackend.Models.Ami;

namespace CtiBackend.Services.Ami;

/// <summary>
/// Parses a single AMI message (text block terminated by blank line) into
/// <see cref="AmiEventEnvelope"/>. Unknown fields are preserved in Raw.
/// </summary>
public sealed class AmiMessageParser : IAmiMessageParser
{
    public AmiEventEnvelope Parse(string raw)
    {
        var env = new AmiEventEnvelope { ReceivedAtUtc = DateTime.UtcNow };
        if (string.IsNullOrWhiteSpace(raw)) return env;

        foreach (var rawLine in raw.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (string.IsNullOrEmpty(line)) continue;
            var idx = line.IndexOf(':');
            if (idx <= 0) continue;
            var key = line.Substring(0, idx).Trim();
            var value = idx + 1 < line.Length ? line.Substring(idx + 1).Trim() : string.Empty;
            env.Raw[key] = value;
        }

        env.Event            = Get(env, "Event");
        env.UserEvent        = Get(env, "UserEvent");
        env.Channel          = Get(env, "Channel");
        env.UniqueId         = Get(env, "Uniqueid") ?? Get(env, "UniqueId");
        env.LinkedId         = Get(env, "Linkedid") ?? Get(env, "LinkedId");
        env.CallerIdNum      = Get(env, "CallerIDNum") ?? Get(env, "CallerIdNum");
        env.ConnectedLineNum = Get(env, "ConnectedLineNum");
        env.Context          = Get(env, "Context");
        env.Exten            = Get(env, "Exten");
        env.Priority         = Get(env, "Priority");
        return env;
    }

    private static string? Get(AmiEventEnvelope e, string key) =>
        e.Raw.TryGetValue(key, out var v) ? v : null;
}
