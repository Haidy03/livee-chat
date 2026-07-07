namespace Outbound.Infrastructure.Ami;

/// <summary>
/// Parses a single AMI message (text block terminated by blank line) into
/// <see cref="AmiEventEnvelope"/>. Unknown fields are preserved in Raw.
/// Repeated <c>ChanVariable: KEY=VAL</c> lines are also exploded into
/// <c>Raw["ChanVariable.KEY"] = VAL</c> so per-channel variables survive
/// last-wins overwrites.
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

            // Explode ChanVariable: KEY=VAL into a stable per-variable key.
            if (string.Equals(key, "ChanVariable", StringComparison.OrdinalIgnoreCase))
            {
                var eq = value.IndexOf('=');
                if (eq > 0)
                {
                    var vKey = value.Substring(0, eq).Trim();
                    var vVal = eq + 1 < value.Length ? value.Substring(eq + 1).Trim() : string.Empty;
                    if (vKey.Length > 0) env.Raw[$"ChanVariable.{vKey}"] = vVal;
                }
            }
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
