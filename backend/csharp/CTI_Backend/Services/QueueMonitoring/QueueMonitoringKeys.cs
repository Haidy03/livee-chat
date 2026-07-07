using CtiBackend.Options;

namespace CtiBackend.Services.QueueMonitoring;

/// <summary>Builds Redis keys for queue monitoring state.</summary>
public sealed class QueueMonitoringKeys
{
    private readonly QueueMonitoringOptions _opts;

    public QueueMonitoringKeys(QueueMonitoringOptions opts) => _opts = opts;

    public string Base(string tenantId, string serverId)
        => $"{_opts.RedisKeyPrefix}:{S(_opts.Environment)}:{S(tenantId)}:{S(serverId)}";

    public string Queues(string t, string s) => $"{Base(t, s)}:queues";
    public string Queue(string t, string s, string q) => $"{Base(t, s)}:queue:{Sanitize(q)}";
    public string QueueMembers(string t, string s, string q) => $"{Queue(t, s, q)}:members";
    public string QueueWaiting(string t, string s, string q) => $"{Queue(t, s, q)}:waiting";
    public string QueueAgent(string t, string s, string q, string a) => $"{Queue(t, s, q)}:agent:{Sanitize(a)}";
    public string QueueCall(string t, string s, string callId) => $"{Base(t, s)}:queue-call:{Sanitize(callId)}";
    public string Agent(string t, string s, string a) => $"{Base(t, s)}:agent:{Sanitize(a)}";
    public string Agents(string t, string s) => $"{Base(t, s)}:agents";
    public string ActiveCall(string t, string s, string callId) => $"{Base(t, s)}:active-call:{Sanitize(callId)}";
    public string SnapshotLock(string t, string s) => $"{Base(t, s)}:queue-snapshot-lock";
    public string EventDedup(string t, string s, string hash) => $"{Base(t, s)}:event-dedup:{hash}";
    public string AmiStatus(string t, string s) => $"{Base(t, s)}:ami-status";

    private static string S(string v) => string.IsNullOrWhiteSpace(v) ? "default" : v.Trim();
    private static string Sanitize(string v) => string.IsNullOrWhiteSpace(v) ? "_" : v.Replace(' ', '_');
}
