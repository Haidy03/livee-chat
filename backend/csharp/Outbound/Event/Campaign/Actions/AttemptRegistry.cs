using System.Collections.Concurrent;
using MongoDB.Driver;
using Outbound.Event.Campaign.Models;
using Outbound.Event.Campaign.Persistence;

namespace Outbound.Event.Campaign.Actions;

/// <summary>
/// Per-attempt context registered by the originator before the AMI socket write and consumed by
/// the outcome finalizer / handlers. Replaces the old TCS-based <c>AttemptCorrelator</c>: nothing
/// blocks on a call outcome now — AMI events drive finalize directly.
/// </summary>
public sealed record AttemptContext(
    string AttemptId,
    string TargetId,
    string CampaignId,
    string TenantId,
    int AttemptNumber,
    string CorrelationId);

public interface IAttemptRegistry
{
    void Register(AttemptContext ctx);
    AttemptContext? TryGet(string attemptId);
    bool Has(string attemptId);
    void Drop(string attemptId);

    /// <summary>Persist a call_attempts milestone update (dial status, hangup cause, AMD, etc.).</summary>
    Task MilestoneAsync(string attemptId, UpdateDefinition<CallAttempt> update, CancellationToken ct);

    void NoteHumanDetected(string attemptId);
    bool WasHumanDetected(string attemptId);
}

public sealed class AttemptRegistry : IAttemptRegistry
{
    private readonly ConcurrentDictionary<string, AttemptContext> _pending = new();
    private readonly ConcurrentDictionary<string, byte> _human = new();
    private readonly ICallAttemptRepository _repo;

    public AttemptRegistry(ICallAttemptRepository repo) { _repo = repo; }

    public void Register(AttemptContext ctx) => _pending[ctx.AttemptId] = ctx;
    public AttemptContext? TryGet(string attemptId) => _pending.TryGetValue(attemptId, out var v) ? v : null;
    public bool Has(string attemptId) => _pending.ContainsKey(attemptId);

    public void Drop(string attemptId)
    {
        _pending.TryRemove(attemptId, out _);
        _human.TryRemove(attemptId, out _);
    }

    public Task MilestoneAsync(string attemptId, UpdateDefinition<CallAttempt> update, CancellationToken ct) =>
        _repo.RecordMilestoneAsync(attemptId, update, ct);

    public void NoteHumanDetected(string attemptId) => _human.TryAdd(attemptId, 0);
    public bool WasHumanDetected(string attemptId) => _human.ContainsKey(attemptId);
}
