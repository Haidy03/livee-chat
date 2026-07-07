using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceFlow.Api.UserMaps.Requests;
using VoiceFlow.Application.Interfaces;
using VoiceFlow.Application.Options;
using VoiceFlow.Contracts.UserMaps.Responses;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Core.Models;
using VoiceFlow.Core.Helpers;
namespace VoiceFlow.Application.Services.UserMap;

/// <summary>
/// Phase 2: snapshot is projected from the live Redis-backed registry that is
/// fed by dialplan ingest events. HTTP contract is identical to the simulator
/// implementation in <see cref="SimulatorUsersMapService"/>.
/// </summary>
public sealed class UsersMapService : IUsersMapService
{
    private const string DefaultTenant = "default";

    private static readonly FlowSummaryResponse SampleFlowTemplate = new()
    {
        Id = "flow_8821",
        Name = "Test (copy)",
        Nodes = new()
        {
            new() { Key = "start",      Label = "start",      Type = "start" },
            new() { Key = "menu_main",  Label = "menu_main",  Type = "menu" },
            new() { Key = "ai_agent",   Label = "ai_agent",   Type = "ai" },
            new() { Key = "queue_main", Label = "queue_main", Type = "queue" },
            new() { Key = "transfer",   Label = "transfer",   Type = "transfer" },
            new() { Key = "survey",     Label = "survey",     Type = "survey" },
            new() { Key = "voicemail",  Label = "voicemail",  Type = "voicemail" },
        },
    };

    private readonly ILiveCallRegistry _registry;
    private readonly ICallControlClient _control;
    private readonly UsersMapOptions _options;
    private readonly ILogger<UsersMapService> _log;

    public UsersMapService(
        ILiveCallRegistry registry,
        ICallControlClient control,
        IOptions<UsersMapOptions> options,
        ILogger<UsersMapService> log)
    {
        _registry = registry;
        _control = control;
        _options = options.Value;
        _log = log;
    }

    public async Task<UsersMapSnapshotResponse> GetSnapshotAsync(string tenantId, CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(tenantId) ? DefaultTenant : tenantId;
        var snap = await _registry.GetSnapshotAsync(key, ct);

        var callers = snap.Calls.Select(MapCaller).ToList();

        // Assign queue positions from the registry's ordered queue.
        var posByCallId = snap.QueueOrder
            .Select((id, i) => (id, pos: i + 1))
            .ToDictionary(x => x.id, x => x.pos);
        foreach (var c in callers)
        {
            if (c.State == "queue" && posByCallId.TryGetValue(c.CallId, out var p))
                c.QueuePosition = p;
        }

        // Count only each caller's current state. Do not use snap.StateCounts here,
        // because those buckets can include stale/history-derived state memberships.
        var currentByState = callers
            .GroupBy(c => c.State ?? string.Empty)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        int CurrentFor(params string[] wires) =>
            wires.Sum(w => currentByState.GetValueOrDefault(w));

        var states = new Dictionary<string, StateBucketResponse>
        {
            ["ivr"]    = new() { Count = CurrentFor("ivr_menu"),        Capacity = _options.StateCapacity.GetValueOrDefault("ivr", 10) },
            ["ai"]     = new() { Count = CurrentFor("ai_agent"),        Capacity = _options.StateCapacity.GetValueOrDefault("ai", 10) },
            ["agent"]  = new() { Count = CurrentFor("call_forwarding"), Capacity = _options.StateCapacity.GetValueOrDefault("agent", 10) },
            ["queue"]  = new() { Count = CurrentFor("queue"),           Capacity = _options.StateCapacity.GetValueOrDefault("queue", 10) },
            ["vm"]     = new() { Count = CurrentFor("voicemail"),       Capacity = _options.StateCapacity.GetValueOrDefault("vm", 10) },
            ["survey"] = new() { Count = CurrentFor("survey"),          Capacity = _options.StateCapacity.GetValueOrDefault("survey", 10) },
        };

        var flow = new FlowSummaryResponse
        {
            Id = SampleFlowTemplate.Id,
            Name = SampleFlowTemplate.Name,
            Nodes = SampleFlowTemplate.Nodes.Select(n => new FlowNodeResponse
            {
                Key = n.Key,
                Label = n.Label,
                Type = n.Type,
                Count = n.Key switch
                {
                    "start" => CurrentFor("call_start"),
                    "menu_main" => CurrentFor("ivr_menu"),
                    "ai_agent" => CurrentFor("ai_agent"),
                    "transfer" => CurrentFor("call_forwarding"),
                    "queue_main" => CurrentFor("queue"),
                    "voicemail" => CurrentFor("voicemail"),
                    "survey" => CurrentFor("survey"),
                    _ => 0,
                },
            }).ToList(),
        };

        return new UsersMapSnapshotResponse
        {
            GeneratedAt = DateTimeOffset.UtcNow.ToString("O"),
            Metrics = new UsersMapMetricsResponse
            {
                ActiveNow = callers.Count,
                InQueue = snap.QueueOrder.Count,
                LongestWaitSec = snap.LongestWaitSec,
                AvgHandleSec = snap.AvgHandleSec,
                AbandonedToday = snap.AbandonedToday,
                SlaPercent = snap.SlaPercent,
                SlaTargetPercent = snap.SlaTargetPercent,
            },
            States = states,
            Callers = callers,
            Flow = flow,
        };
    }

    public async Task<bool> ActionAsync(string tenantId, string callId, string kind, TransferRequest? transfer, CancellationToken ct)
    {
        var key = string.IsNullOrWhiteSpace(tenantId) ? DefaultTenant : tenantId;
        return kind.ToLowerInvariant() switch
        {
            "listen"   => await _control.ListenAsync(key, callId, ct),
            "whisper"  => await _control.WhisperAsync(key, callId, ct),
            "barge"    => await _control.BargeAsync(key, callId, ct),
            "hangup"   => await _control.HangupAsync(key, callId, ct),
            "transfer" => await _control.TransferAsync(
                key, callId,
                transfer?.Target.Type ?? "agent",
                transfer?.Target.Id,
                transfer?.Target.Number, ct),
            _ => false,
        };
    }

    private static CallerResponse MapCaller(LiveCallRecord c) => new()
    {
        Id = c.Id,
        CallId = c.CallId,
        Name = c.Name,
        MaskedNumber = c.MaskedNumber,
        Color = c.Color,
        State = c.State.ToWire(),
        EnteredStateAt = c.EnteredStateAt,
        CallStartedAt = c.CallStartedAt,
        IvrChoice = c.IvrChoice,
        Intent = c.Intent,
        Detail = c.Detail,
        Agent = c.Agent == null ? null : new AgentRefResponse { Id = c.Agent.Id, Name = c.Agent.Name },
        QueuePosition = c.QueuePosition,
        SurveyStep = c.SurveyStep,
        SurveyTotal = c.SurveyTotal,
        Channel = c.Channel,
        Direction = c.Direction,
        Tags = c.Tags?.ToList(),
        History = c.History.Select(h => new JourneyStepResponse
        {
            At = h.At,
            NodeKey = h.NodeKey,
            Kind = h.Kind,
            Label = string.IsNullOrEmpty(h.Label) ? h.NodeKey : h.Label,
            Sub = h.Sub,
        }).ToList(),
    };
}
