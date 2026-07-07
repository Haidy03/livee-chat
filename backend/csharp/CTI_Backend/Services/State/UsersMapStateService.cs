using Microsoft.Extensions.Logging;

namespace CtiBackend.Services.State.UsersMap;

/// <summary>
/// Hosts the EXACT logic copied from UsersMapIngestController.State.
/// The body of <see cref="ApplyStateAsync"/> below is a verbatim copy of the
/// controller method — do not change the logic, models, behavior, property
/// names, or order of operations.
/// </summary>
public sealed class UsersMapStateService
{
    private readonly ILiveCallRegistry _registry;
    private readonly ICallRecordRepository _callRecords;
    private readonly ILogger<UsersMapStateService> _log;

    public UsersMapStateService(
        ILiveCallRegistry registry,
        ICallRecordRepository callRecords,
        ILogger<UsersMapStateService> log)
    {
        _registry = registry;
        _callRecords = callRecords;
        _log = log;
    }

    public sealed class StateResult
    {
        public bool BadRequest { get; init; }
        public string? Error { get; init; }
        public bool Finalized { get; init; }
        public bool Ok => !BadRequest && Error is null;
    }

    // ========================================================================
    // BEGIN: VERBATIM COPY of UsersMapIngestController.State
    // Source: backend/csharp/ReportsService/.../UsersMapIngestController.cs
    // DO NOT modify logic, models, property names, or behavior.
    // The only changes are: return type swapped from ActionResult to
    // StateResult, and the response shape lifted into StateResult fields.
    // ========================================================================
    public async Task<StateResult> ApplyStateAsync(IngestStateRequest body, CancellationToken ct)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.CallId))
            return new StateResult { BadRequest = true, Error = "callId required" };

        var nowIso = DateTimeOffset.UtcNow.ToString("O");
        var atIso = string.IsNullOrEmpty(body.At) ? nowIso : body.At;
        var state = CallStateExtensions.TryParseWire(body.State, out var parsed)
            ? parsed
            : NodeKindMap.ToState(body.Node?.Kind, _log);

        var call = new LiveCall
        {
            Id = body.CallId,
            CallId = body.CallId,
            TenantId = body.TenantId,
            Name = body.Caller?.Name ?? "Unknown",
            MaskedNumber = body.Caller?.Number ?? string.Empty,
            Color = body.Color ?? "#647590",
            State = state,
            EnteredStateAt = atIso,
            CallStartedAt = atIso, // overwritten below when an earlier value exists
            FlowId = body.Node?.FlowId,
            NodeKey = body.Node?.Key,
            NodeKind = body.Node?.Kind,
            NodeLabel = body.Node?.Label,
            IvrChoice = body.IvrChoice,
            Intent = body.Intent,
            Agent = body.Agent == null ? null : new LiveAgentRef { Id = body.Agent.Id, Name = body.Agent.Name },
            QueuePosition = body.QueuePosition,
            SurveyStep = body.Survey?.Step,
            SurveyTotal = body.Survey?.Total,
            Channel = body.Channel ?? "voice",
            Direction = body.Direction ?? "inbound",
            Tags = body.Tags,
            History = new List<LiveJourneyStep>(),
        };

        // Preserve call-start timestamp + history across transitions by reading the prior record.
        var existing = await ReadExistingAsync(body.TenantId, body.CallId, ct);
        if (existing != null)
        {
            call.CallStartedAt = existing.CallStartedAt;
            call.History = existing.History ?? new List<LiveJourneyStep>();
            // Carry forward fields that aren't re-asserted on this event.
            if (string.IsNullOrEmpty(call.Name) || call.Name == "Unknown") call.Name = existing.Name;
            if (string.IsNullOrEmpty(call.MaskedNumber)) call.MaskedNumber = existing.MaskedNumber;
            if (string.IsNullOrEmpty(call.Color) || call.Color == "#647590") call.Color = existing.Color;
            if (call.Agent == null) call.Agent = existing.Agent;
            if (call.Tags == null) call.Tags = existing.Tags;
        }

        call.History.Add(new LiveJourneyStep
        {
            At = atIso,
            NodeKey = body.Node?.Key ?? state.ToWire(),
            Kind = body.Node?.Kind,
            Label = body.Node?.Label ?? body.Node?.Key ?? state.ToWire(),
            Sub = body.IvrChoice ?? body.Intent,
        });

        // Terminal state: persist a finalized CallRecord to Mongo and drop
        // the call from Redis so it disappears from the live Users Map.
        if (state == CallState.EndCall)
        {
            var record = BuildRecord(call, atIso, "completed");
            try
            {
                await _callRecords.AddAsync(record, ct);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to persist CallRecord for {CallId}", call.CallId);
            }
            await _registry.RemoveAsync(call.TenantId, call.CallId, "completed", ct);
            return new StateResult { Finalized = true };
        }

        await _registry.RecordStateAsync(call, ct);
        return new StateResult();
    }

    private static CallRecord BuildRecord(LiveCall call, string endedAtIso, string reason)
    {
        var durationSec = 0;
        if (DateTimeOffset.TryParse(call.CallStartedAt, out var started) &&
            DateTimeOffset.TryParse(endedAtIso, out var ended))
        {
            durationSec = (int)Math.Max(0, (ended - started).TotalSeconds);
        }

        return new CallRecord
        {
            TenantId = call.TenantId,
            CallId = call.CallId,
            Name = call.Name,
            MaskedNumber = call.MaskedNumber,
            Color = call.Color,
            FinalState = call.State.ToWire(),
            Reason = reason,
            CallStartedAt = call.CallStartedAt,
            EndedAt = endedAtIso,
            DurationSec = durationSec,
            FlowId = call.FlowId,
            NodeKey = call.NodeKey,
            NodeKind = call.NodeKind,
            NodeLabel = call.NodeLabel,
            IvrChoice = call.IvrChoice,
            Intent = call.Intent,
            Detail = call.Detail,
            Agent = call.Agent,
            QueuePosition = call.QueuePosition,
            SurveyStep = call.SurveyStep,
            SurveyTotal = call.SurveyTotal,
            Channel = call.Channel,
            Direction = call.Direction,
            Tags = call.Tags,
            History = call.History ?? new List<LiveJourneyStep>(),
        };
    }

    private async Task<LiveCall?> ReadExistingAsync(string tenantId, string callId, CancellationToken ct)
    {
        try
        {
            var snap = await _registry.GetSnapshotAsync(tenantId, ct);
            return snap.Calls.FirstOrDefault(c => c.CallId == callId);
        }
        catch
        {
            return null;
        }
    }
    // ========================================================================
    // END: VERBATIM COPY
    // ========================================================================
}
