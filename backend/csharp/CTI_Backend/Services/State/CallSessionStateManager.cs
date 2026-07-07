using System.Collections.Concurrent;
using CtiBackend.Models.Ami;
using CtiBackend.Models.Cti;
using CtiBackend.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CtiBackend.Services.State;

/// <summary>
/// Thread-safe in-memory store of <see cref="CallSessionState"/>. Every AMI
/// event is mapped here; ended sessions are kept for a retention window so
/// the frontend polling endpoints can still surface them.
/// </summary>
public sealed class CallSessionStateManager : ICallSessionStateManager
{
    private readonly ConcurrentDictionary<string, CallSessionState> _sessions = new();
    private readonly ConcurrentDictionary<string, byte> _locks = new();
    private readonly ILogger<CallSessionStateManager> _log;
    private readonly SessionRetentionOptions _retention;

    public CallSessionStateManager(
        IOptions<SessionRetentionOptions> retention,
        ILogger<CallSessionStateManager> log)
    {
        _retention = retention.Value;
        _log = log;
    }

    public CallSessionState ApplyEvent(AmiEventEnvelope env)
    {
        var key = DeriveKey(env);
        var session = _sessions.GetOrAdd(key, k => new CallSessionState
        {
            SessionId = k,
            StartedAtUtc = DateTime.UtcNow,
        });

        lock (LockFor(key))
        {
            session.LinkedId ??= env.LinkedId;
            session.UniqueId ??= env.UniqueId;
            session.CallId   ??= env.Raw.TryGetValue("CallId", out var cid) ? cid : null;
            session.LastUpdatedAtUtc = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(env.Channel)) session.CurrentChannel = env.Channel;
            if (!string.IsNullOrWhiteSpace(env.Context)) session.CurrentContext = env.Context;
            if (!string.IsNullOrWhiteSpace(env.Exten))   session.CurrentExtension = env.Exten;
            if (string.IsNullOrWhiteSpace(session.CallerNumber) && !string.IsNullOrWhiteSpace(env.CallerIdNum))
                session.CallerNumber = env.CallerIdNum;

            var evt = env.Event ?? "Unknown";
            try
            {
                switch (evt)
                {
                    case "Newchannel":
                        if (env.Raw.TryGetValue("ChannelStateDesc", out var desc)) session.CurrentState = desc;
                        AddJourney(session, evt, env);
                        break;
                    case "Newstate":
                        if (env.Raw.TryGetValue("ChannelStateDesc", out var st)) session.CurrentState = st;
                        AddJourney(session, evt, env);
                        break;
                    case "DialBegin":
                    case "DialEnd":
                    case "BridgeEnter":
                    case "BridgeLeave":
                        AddJourney(session, evt, env);
                        break;
                    case "QueueCallerJoin":
                        if (env.Raw.TryGetValue("Queue", out var q)) session.CurrentQueue = q;
                        AddJourney(session, evt, env, queue: session.CurrentQueue);
                        break;
                    case "QueueCallerLeave":
                        AddJourney(session, evt, env, queue: session.CurrentQueue);
                        break;
                    case "AgentCalled":
                        if (env.Raw.TryGetValue("AgentCalled", out var ac)) session.CurrentAgent = ac;
                        AddJourney(session, evt, env, agent: session.CurrentAgent);
                        break;
                    case "AgentConnect":
                        if (env.Raw.TryGetValue("Channel", out var ach)) session.CurrentAgent = ach;
                        session.CurrentState = "AgentConnected";
                        AddJourney(session, evt, env, agent: session.CurrentAgent);
                        break;
                    case "AgentComplete":
                        session.CurrentState = "AgentComplete";
                        AddJourney(session, evt, env, agent: session.CurrentAgent);
                        break;
                    case "DTMFBegin":
                    case "DTMFEnd":
                        if (env.Raw.TryGetValue("Digit", out var d))
                        {
                            session.LastDigit = d;
                            AddJourney(session, evt, env, digit: d);
                        }
                        else AddJourney(session, evt, env);
                        break;
                    case "Hangup":
                        session.CurrentState = "Hangup";
                        session.IsEnded = true;
                        session.EndedAtUtc = DateTime.UtcNow;
                        AddJourney(session, evt, env);
                        break;
                    case "UserEvent":
                        ApplyUserEvent(session, env);
                        break;
                    case "VarSet":
                        // pass through into metadata
                        if (env.Raw.TryGetValue("Variable", out var vn) &&
                            env.Raw.TryGetValue("Value", out var vv))
                            session.Metadata[vn] = vv;
                        break;
                    default:
                        AddJourney(session, evt, env);
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error applying AMI event {Event} to session {Session}", evt, key);
            }
        }

        EvictOldEnded();
        return session;
    }

    private static void ApplyUserEvent(CallSessionState session, AmiEventEnvelope env)
    {
        var name = env.UserEvent ?? env.Raw.GetValueOrDefault("UserEvent");
        var je = new CallJourneyEvent
        {
            EventName = "UserEvent",
            UserEventName = name,
            TimestampUtc = DateTime.UtcNow,
            Raw = new Dictionary<string, string>(env.Raw, StringComparer.OrdinalIgnoreCase),
        };
        if (env.Raw.TryGetValue("NodeId", out var nid))   { session.CurrentNodeId = nid; je.NodeId = nid; }
        if (env.Raw.TryGetValue("NodeType", out var nt))  { session.CurrentNodeType = nt; je.NodeType = nt; }
        if (env.Raw.TryGetValue("Digit", out var dg))     { session.LastDigit = dg; je.Digit = dg; }
        if (env.Raw.TryGetValue("Tenant", out var tid)) session.TenantId = tid;
        if (env.Raw.TryGetValue("FlowId", out var fid))   session.Metadata["FlowId"] = fid;
        if (env.Raw.TryGetValue("FlowVersion", out var fv)) session.Metadata["FlowVersion"] = fv;
        if (env.Raw.TryGetValue("CustomerId", out var cid)) session.Metadata["CustomerId"] = cid;
        if (env.Raw.TryGetValue("CustomerName", out var cn)) session.CallerName = cn;
        if (env.Raw.TryGetValue("CustomerType", out var ct)) session.CallerType = ct;
        if (env.Raw.TryGetValue("IsVip", out var vip) && bool.TryParse(vip, out var bvip)) session.IsVip = bvip;
        if (env.Raw.TryGetValue("Caller", out var caller) && string.IsNullOrWhiteSpace(session.CallerNumber))
            session.CallerNumber = caller;
        if (env.Raw.TryGetValue("CallId", out var callId) && string.IsNullOrWhiteSpace(session.CallId))
            session.CallId = callId;
        session.Journey.Add(je);
    }

    private static void AddJourney(CallSessionState session, string evt, AmiEventEnvelope env,
        string? digit = null, string? queue = null, string? agent = null)
    {
        session.Journey.Add(new CallJourneyEvent
        {
            EventName = evt,
            TimestampUtc = DateTime.UtcNow,
            Digit = digit,
            Queue = queue,
            Agent = agent,
            Raw = new Dictionary<string, string>(env.Raw, StringComparer.OrdinalIgnoreCase),
        });
    }

    private object LockFor(string key)
    {
        _locks.TryAdd(key, 0);
        return _locks;
    }

    public static string DeriveKey(AmiEventEnvelope env)
    {
        if (!string.IsNullOrWhiteSpace(env.LinkedId)) return env.LinkedId!;
        if (!string.IsNullOrWhiteSpace(env.UniqueId)) return env.UniqueId!;
        if (env.Raw.TryGetValue("CallId", out var cid) && !string.IsNullOrWhiteSpace(cid)) return cid;
        if (!string.IsNullOrWhiteSpace(env.Channel)) return env.Channel!;
        return "evt-" + Guid.NewGuid().ToString("N");
    }

    public IReadOnlyList<CallSessionState> Active() =>
        _sessions.Values.Where(s => !s.IsEnded).ToList();

    public IReadOnlyList<CallSessionState> RecentEnded() =>
        _sessions.Values.Where(s => s.IsEnded).OrderByDescending(s => s.EndedAtUtc).ToList();

    public CallSessionState? GetById(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var s) ? s : null;

    public CallSessionState? GetByLinkedId(string linkedId) =>
        _sessions.Values.FirstOrDefault(s => s.LinkedId == linkedId);

    public CallSessionState? GetByUniqueId(string uniqueId) =>
        _sessions.Values.FirstOrDefault(s => s.UniqueId == uniqueId);

    public IReadOnlyList<CallSessionState> GetByCaller(string callerNumber) =>
        _sessions.Values.Where(s => s.CallerNumber == callerNumber).ToList();

    public void UpdateCallerInfo(string sessionId, CallerInfoModel info)
    {
        if (!_sessions.TryGetValue(sessionId, out var s)) return;
        lock (LockFor(sessionId))
        {
            if (!string.IsNullOrWhiteSpace(info.Name)) s.CallerName = info.Name;
            if (!string.IsNullOrWhiteSpace(info.Type)) s.CallerType = info.Type;
            s.IsVip = info.IsVip;
            if (!string.IsNullOrWhiteSpace(info.CustomerId)) s.Metadata["CustomerId"] = info.CustomerId;
            if (!string.IsNullOrWhiteSpace(info.AccountNumber)) s.Metadata["AccountNumber"] = info.AccountNumber;
            if (!string.IsNullOrWhiteSpace(info.NationalId)) s.Metadata["NationalId"] = info.NationalId;
            if (!string.IsNullOrWhiteSpace(info.Segment)) s.Metadata["Segment"] = info.Segment;
            s.LastUpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private void EvictOldEnded()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-Math.Max(1, _retention.RecentEndedMinutes));
        foreach (var kv in _sessions)
        {
            if (kv.Value.IsEnded && kv.Value.EndedAtUtc.HasValue && kv.Value.EndedAtUtc.Value < cutoff)
                _sessions.TryRemove(kv.Key, out _);
        }
    }
}
