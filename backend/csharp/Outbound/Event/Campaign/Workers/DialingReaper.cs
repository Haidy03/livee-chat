using Microsoft.Extensions.Options;
using Outbound.Event.Campaign.Actions;
using Outbound.Event.Campaign.Ami;
using Outbound.Event.Campaign.Models;
using Outbound.Event.Campaign.Options;
using Outbound.Event.Campaign.Pacing;
using Outbound.Event.Campaign.Persistence;
using Outbound.Infrastructure;

namespace Outbound.Event.Campaign.Workers;

/// <summary>
/// Second-line safety net. Every ~30s (leader-gated), sweeps for stale <c>dialing</c> rows whose
/// <c>dialingAt</c> is older than <see cref="ReaperOptions.MaxCallDurationSeconds"/> — those are
/// crashes, missed hangups, or lost AMI events. It finalizes each as an <c>unknown</c> retry
/// (which the finalizer maps: infra-style retry that doesn't burn an attempt) and gives the
/// concurrency token back. It also periodically reconciles <c>conc:campaign:{id}</c> against the
/// actual count of dialing rows so a missed DECR can't permanently starve a campaign.
/// </summary>
public sealed class DialingReaper : ILeaderGatedWorker
{
    private readonly CampaignRepository _repo;
    private readonly IAttemptRegistry _registry;
    private readonly IOutcomeFinalizer _finalizer;
    private readonly IConcurrencyCounter _concurrency;
    private readonly ReaperOptions _opt;
    private readonly ILogger<DialingReaper> _log;

    public DialingReaper(
        CampaignRepository repo,
        IAttemptRegistry registry,
        IOutcomeFinalizer finalizer,
        IConcurrencyCounter concurrency,
        IOptions<ReaperOptions> opt,
        ILogger<DialingReaper> log)
    {
        _repo = repo;
        _registry = registry;
        _finalizer = finalizer;
        _concurrency = concurrency;
        _opt = opt.Value;
        _log = log;
    }

    public string LeaseId => "dialing-reaper";
    public TimeSpan SweepInterval => TimeSpan.FromSeconds(Math.Max(5, _opt.SweepIntervalSeconds));

    public async Task RunSweepAsync(CancellationToken ct)
    {
        var stale = await _repo.GetStaleDialingTargetsAsync(
            TimeSpan.FromSeconds(_opt.MaxCallDurationSeconds), limit: 200, ct);

        foreach (var t in stale)
        {
            if (ct.IsCancellationRequested) break;

            var attemptId = t.AttemptId;
            if (string.IsNullOrWhiteSpace(attemptId))
            {
                // No attempt id → we can't correlate. Move to failed for review.
                _log.LogWarning("Reaping dialing target {TargetId} with no attemptId → failed.", t.Id);
                await _repo.FailTargetAsync(t.Id, "needs_review", "stale dialing, no attemptId", ct);
                await _concurrency.GiveBackAsync(t.CampaignId, ct);
                continue;
            }

            if (_registry.Has(attemptId))
            {
                // The registry still tracks it — finalize via the standard path so the counter
                // and target flip stay in sync with a normal AMI outcome.
                await _finalizer.FinalizeAsync(attemptId!, TargetActionResult.Retry(
                    disposition: "reaped", reason: "stale dialing", isInfrastructure: true), ct);
            }
            else
            {
                // Registry is gone (process restarted after crash). Retry as infra without burning
                // an attempt; give the counter back to unstick the campaign.
                var backoff = DateTime.UtcNow.AddSeconds(30);
                await _repo.ScheduleRetryAsync(t.Id, backoff, "reaped", "stale dialing, no registry", countsAsAttempt: false, ct);
                await _concurrency.GiveBackAsync(t.CampaignId, ct);
            }
        }

        // Reconcile counters for campaigns that had reaped targets so a missed DECR heals now.
        var campaignIds = stale.Select(t => t.CampaignId).Distinct().ToArray();
        foreach (var id in campaignIds)
        {
            try
            {
                var actual = await _repo.CountDialingAsync(id, ct);
                await _concurrency.SetCountAsync(id, actual, ct);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Reaper: counter reconcile failed for {CampaignId}", id);
            }
        }

        if (stale.Count > 0)
            _log.LogInformation("Reaper handled {N} stale dialing rows.", stale.Count);
    }
}
