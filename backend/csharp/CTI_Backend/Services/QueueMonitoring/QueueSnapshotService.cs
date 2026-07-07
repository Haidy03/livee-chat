using System.Collections.Concurrent;
using CtiBackend.Models.Ami;
using CtiBackend.Options;
using CtiBackend.Services.Ami;
using CtiBackend.Services.QueueMonitoring.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CtiBackend.Services.QueueMonitoring;

public sealed class QueueSnapshotService : IQueueSnapshotService
{
    private readonly IAmiActionSender _sender;
    private readonly IQueueMonitoringRedisRepository _repo;
    private readonly IAgentIdentityNormalizer _normalizer;
    private readonly QueueMonitoringOptions _opts;
    private readonly ILogger<QueueSnapshotService> _log;

    private readonly ConcurrentDictionary<string, QueueSnapshotContext> _tracking = new();

    public QueueSnapshotService(
        IAmiActionSender sender,
        IQueueMonitoringRedisRepository repo,
        IAgentIdentityNormalizer normalizer,
        IOptions<QueueMonitoringOptions> opts,
        ILogger<QueueSnapshotService> log)
    {
        _sender = sender;
        _repo = repo;
        _normalizer = normalizer;
        _opts = opts.Value;
        _log = log;
    }

    public bool IsTracking(string actionId) => _tracking.ContainsKey(actionId);

    public async Task<string> RequestFullSnapshotAsync(AmiConnectionContext ctx, CancellationToken ct)
    {
        if (!_opts.RequestSnapshotOnConnect) return "";

        var token = Guid.NewGuid().ToString("N");
        if (!await _repo.TryAcquireSnapshotLockAsync(ctx.TenantId, ctx.ServerId,
                TimeSpan.FromSeconds(_opts.SnapshotLockSeconds), token, ct))
        {
            _log.LogInformation("Snapshot already in progress for {Tenant}/{Server}", ctx.TenantId, ctx.ServerId);
            return "";
        }

        var statusActionId = $"qstatus-{Guid.NewGuid():N}";
        var summaryActionId = $"qsummary-{Guid.NewGuid():N}";

        var statusCtx = new QueueSnapshotContext { ActionId = statusActionId, TenantId = ctx.TenantId, ServerId = ctx.ServerId };
        _tracking[statusActionId] = statusCtx;
        if (_opts.RequestSummaryOnConnect)
            _tracking[summaryActionId] = new QueueSnapshotContext { ActionId = summaryActionId, TenantId = ctx.TenantId, ServerId = ctx.ServerId };

        try
        {
            if (!_sender.IsReady)
            {
                _log.LogWarning("AMI sender not ready; cannot request snapshot");
                _tracking.TryRemove(statusActionId, out _);
                _tracking.TryRemove(summaryActionId, out _);
                await _repo.ReleaseSnapshotLockAsync(ctx.TenantId, ctx.ServerId, token, ct);
                return "";
            }

            await _sender.SendAsync(new[]
            {
                new KeyValuePair<string, string>("Action", "QueueStatus"),
                new KeyValuePair<string, string>("ActionID", statusActionId),
            }, ct);

            if (_opts.RequestSummaryOnConnect)
            {
                await _sender.SendAsync(new[]
                {
                    new KeyValuePair<string, string>("Action", "QueueSummary"),
                    new KeyValuePair<string, string>("ActionID", summaryActionId),
                }, ct);
            }

            // Background timeout watchdog: release lock & cleanup after timeout.
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(_opts.SnapshotTimeoutSeconds));
                _tracking.TryRemove(statusActionId, out _);
                _tracking.TryRemove(summaryActionId, out _);
                await _repo.ReleaseSnapshotLockAsync(ctx.TenantId, ctx.ServerId, token, CancellationToken.None);
            });

            // Tie the lock token to the status actionId so completion can release it.
            _lockTokens[statusActionId] = token;

            return statusActionId;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to request snapshot");
            _tracking.TryRemove(statusActionId, out _);
            _tracking.TryRemove(summaryActionId, out _);
            await _repo.ReleaseSnapshotLockAsync(ctx.TenantId, ctx.ServerId, token, ct);
            return "";
        }
    }

    private readonly ConcurrentDictionary<string, string> _lockTokens = new();

    public async Task HandleSnapshotEventAsync(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var actionId = env.Raw.GetValueOrDefault("ActionID");
        if (string.IsNullOrEmpty(actionId) || !_tracking.TryGetValue(actionId, out var snap)) return;

        var raw = env.Raw;
        switch (env.Event)
        {
            case "QueueParams":
            case "QueueSummary":
            {
                var name = raw.GetValueOrDefault("Queue") ?? "";
                if (string.IsNullOrEmpty(name)) break;
                var rowTenant = QueueNameParser.TryParse(name, out var t, out _) ? t : ctx.TenantId;
                var q = snap.Queues.GetOrAdd(name, _ => new QueueLiveState
                {
                    TenantId = rowTenant,
                    ServerId = ctx.ServerId,
                    QueueName = name,
                    LastEventUtc = DateTime.UtcNow,
                });
                if (env.Event == "QueueParams")
                {
                    q.Strategy = raw.GetValueOrDefault("Strategy");
                    q.MaxLength = ParseInt(raw.GetValueOrDefault("Max"));
                    q.Completed = ParseLong(raw.GetValueOrDefault("Completed"));
                    q.Abandoned = ParseLong(raw.GetValueOrDefault("Abandoned"));
                }
                else // QueueSummary
                {
                    q.MemberCount = ParseInt(raw.GetValueOrDefault("LoggedIn"));
                    q.AvailableAgentCount = ParseInt(raw.GetValueOrDefault("Available"));
                    q.WaitingCount = ParseInt(raw.GetValueOrDefault("Callers"));
                }
                break;
            }
            case "QueueMember":
            {
                var iface = raw.GetValueOrDefault("Interface") ?? "";
                if (string.IsNullOrEmpty(iface)) break;
                var qname = raw.GetValueOrDefault("Queue");
                var rowTenant = QueueNameParser.TryParse(qname, out var t, out _) ? t : ctx.TenantId;
                var id = _normalizer.Normalize(iface, raw.GetValueOrDefault("StateInterface"), raw.GetValueOrDefault("Name"));
                var statusCode = ParseInt(raw.GetValueOrDefault("Status"));
                var paused = raw.GetValueOrDefault("Paused") == "1";
                var inCall = raw.GetValueOrDefault("InCall") == "1";
                snap.Members.Add(new QueueAgentLiveState
                {
                    TenantId = rowTenant,
                    ServerId = ctx.ServerId,
                    QueueName = qname,
                    AgentId = id.AgentId,
                    Interface = id.Interface,
                    StateInterface = id.StateInterface,
                    MemberName = raw.GetValueOrDefault("Name") ?? raw.GetValueOrDefault("MemberName"),
                    StatusCode = statusCode,
                    Status = AmiStatusMapping.Compute(statusCode, paused, inCall, false),
                    Paused = paused,
                    InCall = inCall,
                    Penalty = ParseInt(raw.GetValueOrDefault("Penalty")),
                    CallsTaken = ParseInt(raw.GetValueOrDefault("CallsTaken")),
                    LastEventUtc = DateTime.UtcNow,
                });
                break;
            }
            case "QueueEntry":
            {
                var qname = raw.GetValueOrDefault("Queue") ?? "";
                var uid = raw.GetValueOrDefault("Uniqueid") ?? "";
                if (string.IsNullOrEmpty(qname) || string.IsNullOrEmpty(uid)) break;
                var rowTenant = QueueNameParser.TryParse(qname, out var t, out _) ? t : ctx.TenantId;
                snap.WaitingCallers.Add(new QueueWaitingCallerState
                {
                    TenantId = rowTenant,
                    ServerId = ctx.ServerId,
                    CallId = uid,
                    UniqueId = uid,
                    LinkedId = raw.GetValueOrDefault("Linkedid"),
                    QueueName = qname,
                    Channel = raw.GetValueOrDefault("Channel"),
                    CallerIdNumber = raw.GetValueOrDefault("CallerIDNum"),
                    CallerIdName = raw.GetValueOrDefault("CallerIDName"),
                    Position = ParseInt(raw.GetValueOrDefault("Position")),
                    JoinedAtUtc = DateTime.UtcNow.AddSeconds(-ParseInt(raw.GetValueOrDefault("Wait"))),
                    LastEventUtc = DateTime.UtcNow,
                });
                break;
            }
            case "QueueStatusComplete":
            case "QueueSummaryComplete":
            {
                _tracking.TryRemove(actionId, out _);

                // Partition the snapshot bundle by tenant derived from queue names,
                // so Redis writes land under the right tenant key prefix even when
                // a single AMI snapshot covers multiple tenants.
                var tenants = new HashSet<string>(StringComparer.Ordinal) { ctx.TenantId };
                foreach (var q in snap.Queues.Values) tenants.Add(q.TenantId);
                foreach (var m in snap.Members) tenants.Add(m.TenantId);
                foreach (var w in snap.WaitingCallers) tenants.Add(w.TenantId);

                foreach (var tenantId in tenants)
                {
                    var partition = new QueueSnapshotContext
                    {
                        ActionId = snap.ActionId,
                        TenantId = tenantId,
                        ServerId = ctx.ServerId,
                        StartedAtUtc = snap.StartedAtUtc,
                    };
                    foreach (var kv in snap.Queues)
                        if (kv.Value.TenantId == tenantId)
                            partition.Queues[kv.Key] = kv.Value;
                    foreach (var m in snap.Members)
                        if (m.TenantId == tenantId) partition.Members.Add(m);
                    foreach (var w in snap.WaitingCallers)
                        if (w.TenantId == tenantId) partition.WaitingCallers.Add(w);

                    if (partition.Queues.IsEmpty && partition.Members.IsEmpty && partition.WaitingCallers.IsEmpty)
                        continue;

                    await _repo.ApplySnapshotAsync(partition, ct);
                    await _repo.UpdateAmiStatusAsync(new AmiServerStatus
                    {
                        TenantId = tenantId,
                        ServerId = ctx.ServerId,
                        Connected = true,
                        ConnectionStatus = "Connected",
                        LastSnapshotUtc = DateTime.UtcNow,
                        LastEventUtc = DateTime.UtcNow,
                        SnapshotStatus = "Synchronized",
                        IsStateStale = false,
                    }, ct);
                    _log.LogInformation("Snapshot applied for {Tenant}/{Server} action={ActionId} queues={Q} members={M} waiting={W}",
                        tenantId, ctx.ServerId, actionId, partition.Queues.Count, partition.Members.Count, partition.WaitingCallers.Count);
                }

                if (_lockTokens.TryRemove(actionId, out var tok))
                    await _repo.ReleaseSnapshotLockAsync(ctx.TenantId, ctx.ServerId, tok, ct);
                break;
            }
        }
    }

    private static int ParseInt(string? s) => int.TryParse(s, out var i) ? i : 0;
    private static long ParseLong(string? s) => long.TryParse(s, out var i) ? i : 0;
}
