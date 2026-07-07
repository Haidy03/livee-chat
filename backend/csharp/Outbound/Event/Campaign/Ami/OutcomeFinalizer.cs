using Microsoft.Extensions.Options;
using Outbound.Event.Campaign.Actions;
using Outbound.Event.Campaign.Models;
using Outbound.Event.Campaign.Options;
using Outbound.Event.Campaign.Pacing;
using Outbound.Event.Campaign.Persistence;
using Outbound.Event.Campaign.Processing;

namespace Outbound.Event.Campaign.Ami;

public interface IOutcomeFinalizer
{
    /// <summary>Idempotent finalize for a terminal AMI outcome. Second call for the same attempt
    /// no-ops (guard on the registry entry existing).</summary>
    Task FinalizeAsync(string attemptId, TargetActionResult result, CancellationToken ct);
}

/// <summary>
/// Owns the retry/fail decision that used to live in <c>CampaignTargetProcessor</c>. Called by the
/// AMI outcome handlers instead of the old TCS resolve path. On finalize it: writes the
/// call_attempts outcome, moves the target to a terminal state or schedules a retry, decrements
/// the concurrency counter (freeing a slot for the next dial), and drops the registry entry.
/// </summary>
public sealed class OutcomeFinalizer : IOutcomeFinalizer
{
    private readonly IAttemptRegistry _registry;
    private readonly CampaignRepository _repo;
    private readonly ICallAttemptRepository _attempts;
    private readonly IConcurrencyCounter _concurrency;
    private readonly CampaignRetryOptions _retry;
    private readonly IDispatcherSignal _signal;
    private readonly ILogger<OutcomeFinalizer> _log;

    public OutcomeFinalizer(
        IAttemptRegistry registry,
        CampaignRepository repo,
        ICallAttemptRepository attempts,
        IConcurrencyCounter concurrency,
        IOptions<CampaignRetryOptions> retry,
        IDispatcherSignal signal,
        ILogger<OutcomeFinalizer> log)
    {
        _registry = registry;
        _repo = repo;
        _attempts = attempts;
        _concurrency = concurrency;
        _retry = retry.Value;
        _signal = signal;
        _log = log;
    }

    public async Task FinalizeAsync(string attemptId, TargetActionResult result, CancellationToken ct)
    {
        // Idempotency: guard on the registry entry existing (a duplicate AMI event finds nothing).
        var ctx = _registry.TryGet(attemptId);
        if (ctx is null)
        {
            Outbound.Infrastructure.Ami.AmiTrace.Write("FINALIZE", $"attempt={attemptId} IGNORED (no registry entry — duplicate/late event)");
            return;
        }

        Outbound.Infrastructure.Ami.AmiTrace.Write("FINALIZE",
            $"attempt={attemptId} target={ctx.TargetId} campaign={ctx.CampaignId} status={result.Status} disposition={result.Disposition} reason={result.Reason} infra={result.IsInfrastructure}");

        // Remove now so a second event returning within this method is a no-op.
        _registry.Drop(attemptId);

        try { await _attempts.FinishAsync(attemptId, result.Disposition, null, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "call_attempts finish failed for {AttemptId}", attemptId); }

        try
        {
            switch (result.Status)
            {
                case TargetActionStatus.Success:
                    await CompleteAsync(ctx, result, ct);
                    break;

                case TargetActionStatus.RetryLater:
                    await RetryAsync(ctx, result, ct);
                    break;

                default:
                    await _repo.FailTargetAsync(ctx.TargetId, result.Disposition, result.Reason, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "OutcomeFinalizer target update failed for {TargetId}", ctx.TargetId);
        }
        finally
        {
            await _concurrency.GiveBackAsync(ctx.CampaignId, ct);
            _signal.Nudge();
        }

        // Best-effort campaign completion check.
        try { await _repo.TryCompleteCampaignAsync(ctx.CampaignId, ct); } catch { /* best effort */ }
    }

    private async Task CompleteAsync(AttemptContext ctx, TargetActionResult result, CancellationToken ct)
    {
        var kind = DispositionMapper.ToTerminal(result.Disposition);
        var status = DispositionMapper.ToStatus(kind);
        if (await _repo.CompleteTargetAsync(ctx.TargetId, "dialing", status, result.Disposition, ct))
            await _repo.IncrementCountersAsync(ctx.CampaignId, status, ct);
    }

    private async Task RetryAsync(AttemptContext ctx, TargetActionResult result, CancellationToken ct)
    {
        // Infrastructure failures don't burn an attempt.
        var countsAsAttempt = !result.IsInfrastructure;
        var newAttempts = countsAsAttempt ? ctx.AttemptNumber : ctx.AttemptNumber - 1;

        if (newAttempts >= _retry.MaxAttempts)
        {
            await _repo.FailTargetAsync(ctx.TargetId, result.Disposition, result.Reason ?? "max attempts exceeded", ct);
            return;
        }

        var delay = result.RetryAfter ?? Backoff(Math.Max(1, newAttempts));
        var nextAttemptAt = DateTime.UtcNow.Add(delay);

        await _repo.ScheduleRetryAsync(ctx.TargetId, nextAttemptAt, result.Disposition, result.Reason, countsAsAttempt, ct);
    }

    private TimeSpan Backoff(int attempt)
    {
        var baseSeconds = _retry.BaseDelaySeconds * Math.Pow(2, Math.Max(0, attempt - 1));
        var capped = Math.Min(baseSeconds, _retry.MaxDelaySeconds);
        var jitter = Random.Shared.NextDouble() * 0.3 * capped;
        return TimeSpan.FromSeconds(capped + jitter);
    }
}

/// <summary>
/// Tiny signal so the finalizer can wake the dispatcher immediately when a token returns.
/// Implementation is a bounded channel writer, connected in the dispatcher.
/// </summary>
public interface IDispatcherSignal
{
    void Nudge();
}
