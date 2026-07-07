using System.Threading.Channels;
using CtiBackend.Models.Ami;
using CtiBackend.Services.CallerInfo;
using CtiBackend.Services.QueueMonitoring;
using CtiBackend.Services.State;
using CtiBackend.Services.State.UsersMap;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CtiBackend.Services.Ami;

/// <summary>
/// Async event consumer. The AMI listener calls <see cref="Enqueue"/> and
/// returns immediately; a background task drains the channel and applies
/// each event to the session state manager and the UsersMapStateService.
/// Exceptions in handlers are caught so one bad event cannot break the loop.
/// </summary>
public sealed class AmiEventDispatcher : BackgroundService, IAmiEventDispatcher
{
    private static readonly HashSet<string> KnownEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "Newchannel","Newstate","DialBegin","DialEnd","BridgeEnter","BridgeLeave",
        "Hangup","QueueCallerJoin","QueueCallerLeave","QueueCallerAbandon",
        "AgentCalled","AgentRingNoAnswer","AgentConnect","AgentComplete",
        "QueueParams","QueueMember","QueueEntry","QueueStatusComplete",
        "QueueSummary","QueueSummaryComplete","QueueMemberStatus","QueueMemberPause",
        "QueueMemberAdded","QueueMemberRemoved","QueueMemberPenalty","QueueMemberRinginuse",
        "VarSet","DTMFBegin","DTMFEnd","UserEvent","Join","Leave",
    };

    private readonly Channel<AmiEventEnvelope> _channel =
        Channel.CreateUnbounded<AmiEventEnvelope>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

    private readonly ICallSessionStateManager _sessions;
    private readonly UsersMapStateService _usersMap;
    private readonly ICallerInfoResolver _callerInfo;
    private readonly IQueueMonitoringEventHandler _queueHandler;
    private readonly AmiConnectionContext _amiCtx;
    private readonly ILogger<AmiEventDispatcher> _log;

    public AmiEventDispatcher(
        ICallSessionStateManager sessions,
        UsersMapStateService usersMap,
        ICallerInfoResolver callerInfo,
        IQueueMonitoringEventHandler queueHandler,
        AmiConnectionContext amiCtx,
        ILogger<AmiEventDispatcher> log)
    {
        _sessions = sessions;
        _usersMap = usersMap;
        _callerInfo = callerInfo;
        _queueHandler = queueHandler;
        _amiCtx = amiCtx;
        _log = log;
    }

    public void Enqueue(AmiEventEnvelope env)
    {
        if (string.IsNullOrWhiteSpace(env.Event))
            return;
        _channel.Writer.TryWrite(env);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var env in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await HandleAsync(env, stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Dispatcher failed for event {Event}", env.Event);
            }
        }
    }

    private async Task HandleAsync(AmiEventEnvelope env, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(env.Event) && !KnownEvents.Contains(env.Event))
            _log.LogDebug("Unknown AMI event {Event}", env.Event);



        if (string.Equals(env.Event, "UserEvent", StringComparison.OrdinalIgnoreCase))
            _log.LogInformation("UserEvent {UserEvent} unique={UniqueId}", env.UserEvent, env.UniqueId);

        var session = _sessions.ApplyEvent(env);



        // Forward to the copied UsersMap state logic.
        try
        {
            var req = await BuildIngestRequest(env, session.TenantId, ct);
            if (req != null && !string.IsNullOrWhiteSpace(req.CallId))
            {
                var result = await _usersMap.ApplyStateAsync(req, ct);
                if (!result.Ok && !result.Finalized)
                    _log.LogDebug("UsersMapStateService skipped: {Error}", result.Error);

                // Resolve caller info on first sight of a phone number — fire-and-forget.
                // if (!string.IsNullOrWhiteSpace(session.CallerNumber) && string.IsNullOrWhiteSpace(session.CallerName))
                if (req.State == "call_start" && !string.IsNullOrWhiteSpace(session.CallerNumber) && string.IsNullOrWhiteSpace(session.CallerName))
                {
                    _ = ResolveCallerInfoSafeAsync(session.SessionId, session.TenantId, session.CallerNumber!);
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "UsersMapStateService failed for {Event}/{Unique}", env.Event, env.UniqueId);
        }

        // Queue monitoring hook. Wrapped so monitoring failures cannot affect
        // the existing CTI pipeline or the AMI listener loop.
        if (QueueMonitoringEvents.IsRelevant(env.Event))
        {
            try { await _queueHandler.HandleAsync(env, _amiCtx, ct); }
            catch (Exception ex) { _log.LogError(ex, "Queue monitoring handler failed for {Event}", env.Event); }
        }
    }

    private async Task ResolveCallerInfoSafeAsync(string sessionId, string? tenantId, string phone, CancellationToken ct = default)
    {
        try
        {
            var info = await _callerInfo.ResolveAsync(tenantId, phone, ct);
            if (info != null) _sessions.UpdateCallerInfo(sessionId, info);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Caller info resolution failed for {Phone}", phone);
        }
    }

    /// <summary>
    /// Maps an AMI envelope to the IngestStateRequest shape understood by
    /// the copied UsersMapStateService.ApplyStateAsync. UserEvent fields
    /// (NodeId, NodeType, FlowId, Caller, CustomerName, etc.) feed the
    /// Node/Caller payload; standard channel events default to the call_start
    /// node kind so the verbatim logic still produces a journey step.
    /// </summary>
    private async Task<IngestStateRequest?> BuildIngestRequest(AmiEventEnvelope env, string? tenantId, CancellationToken ct)
    {
        var raw = env.Raw;

        var isUserEvent = string.Equals(env.Event, "UserEvent", StringComparison.OrdinalIgnoreCase);

        if (isUserEvent == false)
            return null;

        string? state = raw.GetValueOrDefault("UserEvent");

        if (state == null)
            return null;


        string? callId =
            raw.TryGetValue("CallId", out var cid) ? cid :
            !string.IsNullOrWhiteSpace(env.LinkedId) ? env.LinkedId :
            env.UniqueId;



        var nodeKind = raw.GetValueOrDefault("Kind");
        var nodeKey = raw.GetValueOrDefault("Node");
        var nodeLbl = raw.GetValueOrDefault("NodeLabel");

        string resolvedTenant = raw.GetValueOrDefault("Tenant");

        var caller = new IngestCaller
        {
            Name = raw.GetValueOrDefault("CallerName"),
            Number = env.CallerIdNum ?? raw.GetValueOrDefault("Num"),
        };

        // On call_start, enrich the caller from the Light CRM contacts collection
        // (same data as GET /api/v1/contacts) before forwarding to UsersMapStateService.
        if (string.Equals(state, "call_start", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(caller.Number)
            && string.IsNullOrWhiteSpace(caller.Name))
        {
            try
            {
                var info = await _callerInfo.ResolveFromDirectoryAsync(resolvedTenant, caller.Number, ct);
                if (info != null && !string.IsNullOrWhiteSpace(info.Name))
                {
                    caller.Name = info.Name;
                }
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Directory enrichment for call_start failed for {Phone}", caller.Number);
            }
        }

        return new IngestStateRequest
        {
            State = state,
            TenantId = resolvedTenant,
            CallId = callId ?? string.Empty,
            At = DateTimeOffset.UtcNow.ToString("O"),
            Node = new IngestNode
            {
                Key = nodeKey,
                Label = nodeLbl,
                Kind = nodeKind,
                FlowId = raw.GetValueOrDefault("FlowId"),
            },
            Caller = caller,
            IvrChoice = raw.GetValueOrDefault("Digit"),
            Intent = raw.GetValueOrDefault("Intent"),
            Channel = raw.GetValueOrDefault("Channel") ?? "voice",
            Direction = NormalizeDirection(raw.GetValueOrDefault("Direction")),
        };
    }

    private static string GuessNodeKind(string? evt) => evt switch
    {
        "Newchannel" => "call_start",
        "QueueCallerJoin" or "QueueCallerLeave" => "queue",
        "AgentCalled" or "AgentConnect" or "AgentComplete" => "call_forwarding",
        "Hangup" => "end_call",
        _ => "call_start",
    };

    // Canonical direction values across the system: inbound / outbound / internal.
    private static string NormalizeDirection(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "inbound";

        return raw.Trim().ToLowerInvariant() switch
        {
            "inbound" or "incoming" or "in" => "inbound",
            "outbound" or "outgoing" or "out" => "outbound",
            "internal" or "self" => "internal",
            _ => "inbound"
        };
    }
}
