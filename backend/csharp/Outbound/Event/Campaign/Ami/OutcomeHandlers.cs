using MongoDB.Driver;
using Outbound.Event.Campaign.Actions;
using Outbound.Event.Campaign.Lookups;
using Outbound.Event.Campaign.Models;
using Outbound.Infrastructure.Ami;

namespace Outbound.Event.Campaign.Ami;

/// <summary>Pre-answer originate-failed signal — correlated by ActionID (== attemptId).</summary>
public sealed class OriginateResponseHandler : IAmiEventHandler
{
    private readonly IAttemptRegistry _registry;
    private readonly IOutcomeFinalizer _finalizer;
    public OriginateResponseHandler(IAttemptRegistry r, IOutcomeFinalizer f) { _registry = r; _finalizer = f; }

    public async Task HandleAsync(AmiEventEnvelope env, CancellationToken ct)
    {
        if (!string.Equals(env.Event, "OriginateResponse", StringComparison.OrdinalIgnoreCase))
            return;

        var attemptId = env.Raw.GetValueOrDefault("ActionID");
        if (string.IsNullOrWhiteSpace(attemptId) || !_registry.Has(attemptId)) return;

        var response = env.Raw.GetValueOrDefault("Response");
        AmiTrace.Write("ORIG-RESP", $"attempt={attemptId} response={response}");
        if (string.Equals(response, "Failure", StringComparison.OrdinalIgnoreCase))
        {
            var reason = env.Raw.GetValueOrDefault("Reason");
            await _finalizer.FinalizeAsync(attemptId, TargetActionResult.Retry(
                disposition: "originate_failed", reason: $"Reason={reason}", isInfrastructure: true), ct);
        }
    }
}

/// <summary>Pre-answer outcome on the originate leg. Correlated by the ATTEMPT_ID channelvar.</summary>
public sealed class DialEndHandler : IAmiEventHandler
{
    private readonly IAttemptRegistry _registry;
    private readonly IOutcomeFinalizer _finalizer;
    public DialEndHandler(IAttemptRegistry r, IOutcomeFinalizer f) { _registry = r; _finalizer = f; }

    public async Task HandleAsync(AmiEventEnvelope env, CancellationToken ct)
    {
        if (!string.Equals(env.Event, "DialEnd", StringComparison.OrdinalIgnoreCase)) return;

        var attemptId = env.Raw.GetValueOrDefault("ChanVariable.ATTEMPT_ID");
        if (string.IsNullOrWhiteSpace(attemptId) || !_registry.Has(attemptId)) return;

        var status = (env.Raw.GetValueOrDefault("DialStatus") ?? string.Empty).ToUpperInvariant();
        AmiTrace.Write("DIAL-END", $"attempt={attemptId} dialStatus={status}");
        await _registry.MilestoneAsync(attemptId,
            Builders<CallAttempt>.Update.Set(a => a.DialStatus, status), ct);

        switch (status)
        {
            case "NOANSWER":
                await _finalizer.FinalizeAsync(attemptId, TargetActionResult.Success("no_answer"), ct);
                break;
            case "BUSY":
                await _finalizer.FinalizeAsync(attemptId, TargetActionResult.Success("busy"), ct);
                break;
            case "CONGESTION":
            case "CHANUNAVAIL":
                await _finalizer.FinalizeAsync(attemptId, TargetActionResult.Retry(
                    disposition: "congestion", reason: status, isInfrastructure: true), ct);
                break;
            case "ANSWER":
                await _registry.MilestoneAsync(attemptId,
                    Builders<CallAttempt>.Update.Set(a => a.AnsweredAt, DateTime.UtcNow), ct);
                // Post-answer path: wait for UserEvent (CampaignQueueFinished / MachineDetected).
                break;
        }
    }
}

/// <summary>
/// Fallback for the AMD=HANGUP path: dialplan hangs up without a UserEvent. If we already saw
/// HumanDetected and the call hangs up before QueueFinished, treat as caller-dropped callback.
/// </summary>
public sealed class HangupHandler : IAmiEventHandler
{
    private readonly IAttemptRegistry _registry;
    private readonly IOutcomeFinalizer _finalizer;
    public HangupHandler(IAttemptRegistry r, IOutcomeFinalizer f) { _registry = r; _finalizer = f; }

    public async Task HandleAsync(AmiEventEnvelope env, CancellationToken ct)
    {
        if (!string.Equals(env.Event, "Hangup", StringComparison.OrdinalIgnoreCase)) return;

        var attemptId = env.Raw.GetValueOrDefault("ChanVariable.ATTEMPT_ID");
        if (string.IsNullOrWhiteSpace(attemptId) || !_registry.Has(attemptId)) return;

        await _registry.MilestoneAsync(attemptId,
            Builders<CallAttempt>.Update.Set(a => a.HangupCause, env.Raw.GetValueOrDefault("Cause")), ct);

        AmiTrace.Write("HANGUP", $"attempt={attemptId} cause={env.Raw.GetValueOrDefault("Cause")} humanDetected={_registry.WasHumanDetected(attemptId)}");
        if (_registry.WasHumanDetected(attemptId))
            await _finalizer.FinalizeAsync(attemptId, TargetActionResult.Success("amd_hangup"), ct);
    }
}

/// <summary>
/// Reliable "answered" terminal for agent-handled calls. AgentComplete fires when the agent↔caller
/// bridge ends (carrying the originate leg's ATTEMPT_ID), even when the caller hangs up mid-call —
/// so it, not the fragile CampaignQueueFinished UserEvent, is what confirms a real conversation.
/// It runs before the Hangup fallback; once it finalizes, the later Hangup finds no registry entry
/// and is a no-op. Callers that abandon before reaching an agent never produce AgentComplete, so
/// they still fall through to the queue/hangup handlers.
/// </summary>
public sealed class AgentCompleteHandler : IAmiEventHandler
{
    private readonly IAttemptRegistry _registry;
    private readonly IOutcomeFinalizer _finalizer;
    private readonly IAgentLookupRepository _agents;
    public AgentCompleteHandler(IAttemptRegistry r, IOutcomeFinalizer f, IAgentLookupRepository agents)
    {
        _registry = r; _finalizer = f; _agents = agents;
    }

    public async Task HandleAsync(AmiEventEnvelope env, CancellationToken ct)
    {
        if (!string.Equals(env.Event, "AgentComplete", StringComparison.OrdinalIgnoreCase)) return;

        var attemptId = env.Raw.GetValueOrDefault("ChanVariable.ATTEMPT_ID");
        if (string.IsNullOrWhiteSpace(attemptId) || !_registry.Has(attemptId)) return;

        var reason = env.Raw.GetValueOrDefault("Reason");

        // Capture the handling agent. The queue member is PJSIP/{extension}; resolve it to the
        // owning platform userId so campaign reports group by the same agent key as calls.
        var ctx = _registry.TryGet(attemptId);
        var extension = ToExtension(
            env.Raw.GetValueOrDefault("Interface")
            ?? env.Raw.GetValueOrDefault("MemberName")
            ?? env.Raw.GetValueOrDefault("StateInterface"));
        string? agentId = extension;
        if (ctx is not null && extension is not null)
            agentId = await _agents.GetUserIdByExtensionAsync(ctx.TenantId, extension, ct) ?? extension;

        AmiTrace.Write("AGENT-DONE", $"attempt={attemptId} reason={reason} agent={agentId} -> answered");

        var update = Builders<CallAttempt>.Update.Set(a => a.QueueStatus, string.Empty);
        if (!string.IsNullOrWhiteSpace(agentId))
            update = Builders<CallAttempt>.Update.Combine(update,
                Builders<CallAttempt>.Update.Set(a => a.AgentId, agentId));

        await _registry.MilestoneAsync(attemptId, update, ct);
        await _finalizer.FinalizeAsync(attemptId, TargetActionResult.Success("answered"), ct);
    }

    /// <summary>PJSIP/1001-0000012a → 1001 (strip channel suffix and technology prefix).</summary>
    private static string? ToExtension(string? member)
    {
        if (string.IsNullOrWhiteSpace(member)) return null;
        var s = member;
        var dash = s.IndexOf('-');
        if (dash >= 0) s = s[..dash];
        var slash = s.LastIndexOf('/');
        if (slash >= 0) s = s[(slash + 1)..];
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }
}

/// <summary>Post-answer terminal signals from the answered dialplan context.</summary>
public sealed class UserEventHandler : IAmiEventHandler
{
    private readonly IAttemptRegistry _registry;
    private readonly IOutcomeFinalizer _finalizer;
    public UserEventHandler(IAttemptRegistry r, IOutcomeFinalizer f) { _registry = r; _finalizer = f; }

    public async Task HandleAsync(AmiEventEnvelope env, CancellationToken ct)
    {
        if (!string.Equals(env.Event, "UserEvent", StringComparison.OrdinalIgnoreCase)) return;

        var attemptId = env.Raw.GetValueOrDefault("AttemptId");
        if (string.IsNullOrWhiteSpace(attemptId) || !_registry.Has(attemptId)) return;

        AmiTrace.Write("USEREVT", $"attempt={attemptId} userEvent={env.UserEvent}");
        switch (env.UserEvent)
        {
            case "CampaignAnswered":
                await _registry.MilestoneAsync(attemptId,
                    Builders<CallAttempt>.Update.Set(a => a.AnsweredAt, DateTime.UtcNow), ct);
                break;

            case "CampaignHumanDetected":
                _registry.NoteHumanDetected(attemptId);
                await _registry.MilestoneAsync(attemptId,
                    Builders<CallAttempt>.Update.Set(a => a.AmdStatus, "HUMAN"), ct);
                break;

            case "CampaignMachineDetected":
                await _registry.MilestoneAsync(attemptId, Builders<CallAttempt>.Update.Combine(
                    Builders<CallAttempt>.Update.Set(a => a.AmdStatus, "MACHINE"),
                    Builders<CallAttempt>.Update.Set(a => a.AmdCause, env.Raw.GetValueOrDefault("AmdCause"))), ct);
                await _finalizer.FinalizeAsync(attemptId, TargetActionResult.Success("voicemail"), ct);
                break;

            case "CampaignQueueFinished":
                var qStatus = env.Raw.GetValueOrDefault("QueueStatus") ?? string.Empty;
                var waitRaw = env.Raw.GetValueOrDefault("QueueWaitSec");
                int.TryParse(waitRaw, out var wait);
                await _registry.MilestoneAsync(attemptId, Builders<CallAttempt>.Update.Combine(
                    Builders<CallAttempt>.Update.Set(a => a.QueueStatus, qStatus),
                    Builders<CallAttempt>.Update.Set(a => a.QueueWaitSec, wait)), ct);

                if (string.IsNullOrEmpty(qStatus))
                    await _finalizer.FinalizeAsync(attemptId, TargetActionResult.Success("answered"), ct);
                else
                    // TIMEOUT / JOINEMPTY / LEAVEEMPTY / FULL — abandoned; retry as infra.
                    await _finalizer.FinalizeAsync(attemptId, TargetActionResult.Retry(
                        disposition: "abandoned", reason: qStatus, isInfrastructure: true), ct);
                break;
        }
    }
}
