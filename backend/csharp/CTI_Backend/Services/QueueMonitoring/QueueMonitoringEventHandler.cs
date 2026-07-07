using System.Security.Cryptography;
using System.Text;
using CtiBackend.Models.Ami;
using CtiBackend.Options;
using CtiBackend.Services.Ami;
using CtiBackend.Services.QueueMonitoring.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CtiBackend.Services.QueueMonitoring;

public sealed class QueueMonitoringEventHandler : IQueueMonitoringEventHandler
{
    private readonly IQueueMonitoringRedisRepository _repo;
    private readonly IAgentIdentityNormalizer _normalizer;
    private readonly IQueueSnapshotService _snapshot;
    private readonly QueueMonitoringOptions _opts;
    private readonly ILogger<QueueMonitoringEventHandler> _log;

    public QueueMonitoringEventHandler(
        IQueueMonitoringRedisRepository repo,
        IAgentIdentityNormalizer normalizer,
        IQueueSnapshotService snapshot,
        IOptions<QueueMonitoringOptions> opts,
        ILogger<QueueMonitoringEventHandler> log)
    {
        _repo = repo;
        _normalizer = normalizer;
        _snapshot = snapshot;
        _opts = opts.Value;
        _log = log;
    }

    public async Task HandleAsync(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        if (!_opts.Enabled || env.Event is null) return;
        var raw = env.Raw;

        // Derive per-event tenant from the Queue name (t_{tenantId}__q_{queueId}).
        // Falls back to the singleton ctx tenant when the queue name doesn't
        // follow the convention or the event has no Queue field.
        var ctxForEvent = ResolveCtx(ctx, raw.GetValueOrDefault("Queue"));

        // Snapshot events are routed via the snapshot service if ActionID matches.
        if (QueueMonitoringEvents.IsSnapshot(env.Event))
        {
            var actionId = raw.GetValueOrDefault("ActionID");
            if (!string.IsNullOrEmpty(actionId) && _snapshot.IsTracking(actionId))
            {
                await _snapshot.HandleSnapshotEventAsync(env, ctxForEvent, ct);
                return;
            }
        }

        // Update last-event timestamp on AMI status (cheap, fire-and-forget safe).
        try
        {
            await _repo.UpdateAmiStatusAsync(new AmiServerStatus
            {
                TenantId = ctxForEvent.TenantId,
                ServerId = ctxForEvent.ServerId,
                Connected = true,
                ConnectionStatus = "Connected",
                LastEventUtc = DateTime.UtcNow,
                LastConnectedUtc = DateTime.UtcNow,
                SnapshotStatus = "Live",
                IsStateStale = false,
            }, ct);
        }
        catch { /* ignore */ }

        // Build a deterministic dedup hash for counter-affecting events only.
        if (IsCounterEvent(env.Event))
        {
            var hash = ComputeHash(ctxForEvent.ServerId, env);
            if (!await _repo.TrySetDedupAsync(ctxForEvent.TenantId, ctxForEvent.ServerId, hash,
                TimeSpan.FromMinutes(_opts.DeduplicationTtlMinutes), ct))
            {
                _log.LogDebug("Duplicate counter event {Event} hash={Hash} ignored", env.Event, hash);
                return;
            }
        }

        try
        {
            switch (env.Event)
            {
                case "QueueCallerJoin":
                case "Join":
                    await OnCallerJoin(env, ctxForEvent, ct); break;
                case "QueueCallerLeave":
                case "Leave":
                    await OnCallerLeave(env, ctxForEvent, ct); break;
                case "QueueCallerAbandon":
                    await OnCallerAbandon(env, ctxForEvent, ct); break;
                case "AgentCalled":
                    await OnAgentCalled(env, ctxForEvent, ct); break;
                case "AgentRingNoAnswer":
                    await OnAgentRingNoAnswer(env, ctxForEvent, ct); break;
                case "AgentConnect":
                    await OnAgentConnect(env, ctxForEvent, ct); break;
                case "AgentComplete":
                    await OnAgentComplete(env, ctxForEvent, ct); break;
                case "QueueMemberStatus":
                case "QueueMemberAdded":
                case "QueueMemberPause":
                case "QueueMemberPenalty":
                case "QueueMemberRinginuse":
                    await OnMemberStatus(env, ctxForEvent, ct); break;
                case "QueueMemberRemoved":
                    await OnMemberRemoved(env, ctxForEvent, ct); break;
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Queue monitoring failed for {Event}", env.Event);
        }
    }

    private static AmiConnectionContext ResolveCtx(AmiConnectionContext baseCtx, string? queueName)
    {
        if (QueueNameParser.TryParse(queueName, out var t, out _) &&
            !string.Equals(t, baseCtx.TenantId, StringComparison.Ordinal))
        {
            return new AmiConnectionContext
            {
                TenantId = t,
                ServerId = baseCtx.ServerId,
                ConnectionName = baseCtx.ConnectionName,
            };
        }
        return baseCtx;
    }

    private static bool IsCounterEvent(string evt) =>
        evt is "QueueCallerAbandon" or "AgentComplete";

    private static string ComputeHash(string serverId, AmiEventEnvelope env)
    {
        var raw = env.Raw;
        var seed = string.Join("|", new[]
        {
            serverId, env.Event ?? "", raw.GetValueOrDefault("Queue") ?? "",
            env.UniqueId ?? "", env.LinkedId ?? "", raw.GetValueOrDefault("Interface") ?? "",
            raw.GetValueOrDefault("Status") ?? "", raw.GetValueOrDefault("Paused") ?? "",
            raw.GetValueOrDefault("ActionID") ?? "",
        });
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(bytes);
    }

    private string CallId(AmiEventEnvelope env) =>
        env.LinkedId ?? env.UniqueId ?? env.Raw.GetValueOrDefault("Linkedid") ?? env.Raw.GetValueOrDefault("Uniqueid") ?? "";

    private Task OnCallerJoin(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var raw = env.Raw;
        var q = raw.GetValueOrDefault("Queue") ?? "";
        if (string.IsNullOrEmpty(q)) return Task.CompletedTask;
        var caller = new QueueWaitingCallerState
        {
            TenantId = ctx.TenantId,
            ServerId = ctx.ServerId,
            CallId = CallId(env),
            UniqueId = env.UniqueId ?? "",
            LinkedId = env.LinkedId,
            QueueName = q,
            Channel = env.Channel ?? raw.GetValueOrDefault("Channel"),
            CallerIdNumber = env.CallerIdNum ?? raw.GetValueOrDefault("CallerIDNum"),
            CallerIdName = raw.GetValueOrDefault("CallerIDName"),
            Position = ParseInt(raw.GetValueOrDefault("Position")),
            JoinedAtUtc = DateTime.UtcNow,
        };
        return _repo.AddWaitingCallerAsync(caller, ct);
    }

    private Task OnCallerLeave(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var q = env.Raw.GetValueOrDefault("Queue") ?? "";
        if (string.IsNullOrEmpty(q)) return Task.CompletedTask;
        return _repo.RemoveWaitingCallerAsync(ctx.TenantId, ctx.ServerId, q, CallId(env),
            env.Raw.GetValueOrDefault("Reason"), ct);
    }

    private Task OnCallerAbandon(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var raw = env.Raw;
        var q = raw.GetValueOrDefault("Queue") ?? "";
        if (string.IsNullOrEmpty(q)) return Task.CompletedTask;
        return _repo.MarkCallerAbandonedAsync(ctx.TenantId, ctx.ServerId, q, CallId(env),
            ParseNullableInt(raw.GetValueOrDefault("Position")),
            ParseNullableInt(raw.GetValueOrDefault("OriginalPosition")),
            ParseNullableInt(raw.GetValueOrDefault("HoldTime")), ct);
    }

    private Task OnAgentCalled(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var raw = env.Raw;
        var q = raw.GetValueOrDefault("Queue") ?? "";
        var iface = raw.GetValueOrDefault("Interface") ?? "";
        if (string.IsNullOrEmpty(q) || string.IsNullOrEmpty(iface)) return Task.CompletedTask;
        var id = _normalizer.Normalize(iface, raw.GetValueOrDefault("StateInterface"), raw.GetValueOrDefault("MemberName"));
        return _repo.MarkAgentRingingAsync(ctx.TenantId, ctx.ServerId, q, id.AgentId, id.Interface,
            raw.GetValueOrDefault("DestChannel"), CallId(env), env.LinkedId, ct);
    }

    private async Task OnAgentRingNoAnswer(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var raw = env.Raw;
        var q = raw.GetValueOrDefault("Queue") ?? "";
        var iface = raw.GetValueOrDefault("Interface") ?? "";
        if (string.IsNullOrEmpty(iface)) return;
        var id = _normalizer.Normalize(iface, raw.GetValueOrDefault("StateInterface"), raw.GetValueOrDefault("MemberName"));
        // Clear active ringing assignment; status will be recalculated by next QueueMemberStatus.
        await _repo.UpsertQueueAgentAsync(new QueueAgentLiveState
        {
            TenantId = ctx.TenantId,
            ServerId = ctx.ServerId,
            QueueName = string.IsNullOrEmpty(q) ? null : q,
            AgentId = id.AgentId,
            Interface = id.Interface,
            MemberName = id.MemberName,
            Status = "Available",
            StatusCode = 1,
            LastEventUtc = DateTime.UtcNow,
        }, ct);
    }

    private Task OnAgentConnect(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var raw = env.Raw;
        var q = raw.GetValueOrDefault("Queue") ?? "";
        var iface = raw.GetValueOrDefault("Interface") ?? "";
        if (string.IsNullOrEmpty(q) || string.IsNullOrEmpty(iface)) return Task.CompletedTask;
        var id = _normalizer.Normalize(iface, raw.GetValueOrDefault("StateInterface"), raw.GetValueOrDefault("MemberName"));
        return _repo.MarkAgentConnectedAsync(ctx.TenantId, ctx.ServerId, q, id.AgentId, id.Interface,
            raw.GetValueOrDefault("DestChannel"), CallId(env), env.LinkedId,
            ParseNullableInt(raw.GetValueOrDefault("HoldTime")), ct);
    }

    private Task OnAgentComplete(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var raw = env.Raw;
        var q = raw.GetValueOrDefault("Queue") ?? "";
        var iface = raw.GetValueOrDefault("Interface") ?? "";
        if (string.IsNullOrEmpty(q) || string.IsNullOrEmpty(iface)) return Task.CompletedTask;
        var id = _normalizer.Normalize(iface, raw.GetValueOrDefault("StateInterface"), raw.GetValueOrDefault("MemberName"));
        return _repo.MarkAgentCompletedAsync(ctx.TenantId, ctx.ServerId, q, id.AgentId, CallId(env),
            ParseNullableInt(raw.GetValueOrDefault("TalkTime")),
            ParseNullableInt(raw.GetValueOrDefault("HoldTime")),
            raw.GetValueOrDefault("Reason"), ct);
    }

    private Task OnMemberStatus(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var raw = env.Raw;
        var q = raw.GetValueOrDefault("Queue") ?? "";
        var iface = raw.GetValueOrDefault("Interface") ?? "";
        if (string.IsNullOrEmpty(q) || string.IsNullOrEmpty(iface)) return Task.CompletedTask;
        var id = _normalizer.Normalize(iface, raw.GetValueOrDefault("StateInterface"), raw.GetValueOrDefault("MemberName"));
        var statusCode = ParseInt(raw.GetValueOrDefault("Status"));
        var paused = ParseBoolFlag(raw.GetValueOrDefault("Paused"));
        var inCall = ParseBoolFlag(raw.GetValueOrDefault("InCall"));
        var ringInUse = ParseBoolFlag(raw.GetValueOrDefault("Ringinuse"));

        return _repo.UpsertQueueAgentAsync(new QueueAgentLiveState
        {
            TenantId = ctx.TenantId,
            ServerId = ctx.ServerId,
            QueueName = q,
            AgentId = id.AgentId,
            Interface = id.Interface,
            StateInterface = id.StateInterface,
            MemberName = id.MemberName,
            StatusCode = statusCode,
            Status = AmiStatusMapping.Compute(statusCode, paused, inCall, ringing: false),
            Paused = paused,
            PausedReason = raw.GetValueOrDefault("PausedReason") ?? raw.GetValueOrDefault("Reason"),
            InCall = inCall,
            RingInUse = ringInUse,
            Penalty = ParseInt(raw.GetValueOrDefault("Penalty")),
            CallsTaken = ParseInt(raw.GetValueOrDefault("CallsTaken")),
            LastCallUtc = UnixToUtc(raw.GetValueOrDefault("LastCall")),
            LastEventUtc = DateTime.UtcNow,
        }, ct);
    }

    private Task OnMemberRemoved(AmiEventEnvelope env, AmiConnectionContext ctx, CancellationToken ct)
    {
        var raw = env.Raw;
        var q = raw.GetValueOrDefault("Queue") ?? "";
        var iface = raw.GetValueOrDefault("Interface") ?? "";
        if (string.IsNullOrEmpty(q) || string.IsNullOrEmpty(iface)) return Task.CompletedTask;
        var id = _normalizer.Normalize(iface, raw.GetValueOrDefault("StateInterface"), raw.GetValueOrDefault("MemberName"));
        return _repo.RemoveQueueAgentAsync(ctx.TenantId, ctx.ServerId, q, id.AgentId, ct);
    }

    private static int ParseInt(string? s) => int.TryParse(s, out var i) ? i : 0;
    private static int? ParseNullableInt(string? s) => int.TryParse(s, out var i) ? i : (int?)null;
    private static bool ParseBoolFlag(string? s) =>
        s is not null && (s == "1" || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "yes", StringComparison.OrdinalIgnoreCase));
    private static DateTime? UnixToUtc(string? s)
    {
        if (!long.TryParse(s, out var v) || v <= 0) return null;
        return DateTimeOffset.FromUnixTimeSeconds(v).UtcDateTime;
    }
}
