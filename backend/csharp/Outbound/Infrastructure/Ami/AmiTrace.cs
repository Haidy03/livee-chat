namespace Outbound.Infrastructure.Ami;

/// <summary>
/// Dev-only, thread-safe file tracer for the AMI / outcome event pipeline. Writes one timestamped
/// line per call to <c>logs/ami-trace-{yyyyMMdd}.log</c> (UTC, millisecond precision) so the whole
/// call-outcome flow can be followed live with <c>tail -f</c>.
///
/// Disabled by default; enable via config key <c>AmiTrace:Enabled=true</c> (wired at startup in
/// Program.cs). Never throws — a tracer must not be able to break the pipeline. Dev aid, not for
/// production use.
/// </summary>
public static class AmiTrace
{
    private static readonly object Gate = new();
    private static volatile bool _enabled; // off until Configure() runs at startup
    private static readonly string FilePath = BuildPath();

    /// <summary>Enable/disable tracing. Called once at startup from configuration (AmiTrace:Enabled).</summary>
    public static void Configure(bool enabled) => _enabled = enabled;

    private static string BuildPath()
    {
        var dir = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "logs");
        try { Directory.CreateDirectory(dir); } catch { /* fall back to cwd below */ }
        return System.IO.Path.Combine(dir, $"ami-trace-{DateTime.UtcNow:yyyyMMdd}.log");
    }

    /// <summary>Absolute path of the current trace file (for logging at startup).</summary>
    public static string Path => FilePath;

    public static void Write(string category, string message)
    {
        if (!_enabled) return;
        var line = $"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ} [{category,-10}] {message}";
        lock (Gate)
        {
            try { File.AppendAllText(FilePath, line + Environment.NewLine); }
            catch { /* never throw from a tracer */ }
        }
    }

    /// <summary>Log an inbound AMI event with whatever correlation fields it carries.</summary>
    public static void Event(AmiEventEnvelope env)
    {
        if (!_enabled) return;
        string F(string k) => env.Raw.TryGetValue(k, out var v) && !string.IsNullOrEmpty(v) ? $" {k}={v}" : "";
        var corr = F("ActionID") + F("AttemptId") + F("ChanVariable.ATTEMPT_ID") + F("UserEvent")
                 + F("Response") + F("Reason") + F("DialStatus") + F("QueueStatus")
                 + F("Cause") + F("AmdCause") + F("Channel");
        Write("EVENT", $"{env.Event}{corr}");
    }
}
