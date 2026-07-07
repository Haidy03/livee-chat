using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Outbound.Event.Campaign.Actions;
using Outbound.Event.Campaign.Ami;
using Outbound.Event.Campaign.Lookups;
using Outbound.Event.Campaign.Models;
using Outbound.Event.Campaign.Options;
using Outbound.Event.Campaign.Pacing;
using Outbound.Event.Campaign.Persistence;
using Outbound.Event.Campaign.RateLimiting;
using Outbound.Infrastructure;
using Outbound.Infrastructure.Ami;

namespace Outbound.Event.Campaign.Workers;

/// <summary>
/// The one dispatcher. Runs under the existing <c>LeaderWorkerHost</c> so exactly one instance
/// dispatches; standby fails over. Each cycle it loops every active campaign as an independent
/// unit — a capped campaign yields <c>budget = 0</c> and we step to the next, so there is no
/// head-of-line blocking. Cycles are triggered by the finalizer's Nudge (call-end / token return)
/// with a heartbeat fallback.
/// </summary>
public sealed class CampaignDispatcher : ILeaderGatedWorker, IDispatcherSignal
{
    private readonly CampaignRepository _repo;
    private readonly IPacingStrategyFactory _pacing;
    private readonly IAgentAvailabilityTracker _agents;
    private readonly IConcurrencyCounter _concurrency;
    private readonly ICampaignStatsProvider _stats;
    private readonly CampaignRateLimiter _rate;
    private readonly IOriginator _originator;
    private readonly IAmiActionSender _ami;
    private readonly CampaignRetryOptions _retry;
    private readonly DispatcherOptions _dispatcher;
    private readonly ILogger<CampaignDispatcher> _log;

    private readonly Channel<byte> _nudges =
        Channel.CreateBounded<byte>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public CampaignDispatcher(
        CampaignRepository repo,
        IPacingStrategyFactory pacing,
        IAgentAvailabilityTracker agents,
        IConcurrencyCounter concurrency,
        ICampaignStatsProvider stats,
        CampaignRateLimiter rate,
        IOriginator originator,
        IAmiActionSender ami,
        IOptions<CampaignRetryOptions> retry,
        IOptions<DispatcherOptions> dispatcher,
        ILogger<CampaignDispatcher> log)
    {
        _repo = repo;
        _pacing = pacing;
        _agents = agents;
        _concurrency = concurrency;
        _stats = stats;
        _rate = rate;
        _originator = originator;
        _ami = ami;
        _retry = retry.Value;
        _dispatcher = dispatcher.Value;
        _log = log;
    }

    public string LeaseId => "campaign-dispatcher";
    public TimeSpan SweepInterval => TimeSpan.FromMilliseconds(Math.Max(100, _dispatcher.HeartbeatMilliseconds));

    /// <summary>Wake the dispatcher immediately (called by <see cref="OutcomeFinalizer"/>).</summary>
    public void Nudge() { _nudges.Writer.TryWrite(0); }

    public async Task RunSweepAsync(CancellationToken ct)
    {
        // Wait for either a nudge or the heartbeat before dispatching. First call after leader
        // acquisition falls through immediately because the channel is empty and the heartbeat
        // is the outer loop's cadence, which is fine.
        _ = _nudges.Reader.TryRead(out _);

        if (!_ami.IsReady)
        {
            _log.LogDebug("AMI not ready — skipping cycle.");
            return;
        }

        var campaigns = await _repo.GetActiveCampaignsAsync(ct);
        if (campaigns.Count == 0) return;

        var totalOriginated = 0;

        foreach (var campaign in campaigns)
        {
            if (ct.IsCancellationRequested) break;
            if (!string.Equals(campaign.Status, "active", StringComparison.OrdinalIgnoreCase)) continue;

            var queueName = QueueNameResolver.Resolve(campaign);
            if (string.IsNullOrWhiteSpace(queueName))
            {
                _log.LogWarning("Campaign {CampaignId} misconfigured (assignedMode=queue with blank QueueId) — skipping.", campaign.Id);
                continue;
            }

            var free = await SafeFreeAgentsAsync(campaign, ct);
            var inFlight = await _concurrency.GetCountAsync(campaign.Id, ct);
            var stats = await _stats.GetAsync(campaign.Id, ct);
            var strategy = _pacing.For(campaign.DialingMode);
            var capacity = strategy.ComputeReleaseBudget(new PacingContext(campaign, free, inFlight, stats));
            if (capacity <= 0) continue;

            var headroom = (int)Math.Max(0, capacity - inFlight);
            var budget = Math.Min(headroom, _dispatcher.MaxPerCampaignPerCycle);
            if (budget <= 0) continue;

            var originated = await DrainCampaignAsync(campaign, queueName!, capacity, budget, ct);
            totalOriginated += originated;

            if (originated == 0)
            {
                // No dialable targets → try to complete (no-op if any are still in-flight).
                try { await _repo.TryCompleteCampaignAsync(campaign.Id, ct); } catch { /* best effort */ }
            }
        }

        if (totalOriginated > 0)
            _log.LogInformation("Dispatcher originated {N} calls across {C} campaigns.", totalOriginated, campaigns.Count);
    }

    private async Task<int> DrainCampaignAsync(CampaignModel campaign, string queueName, int capacityCap, int budget, CancellationToken ct)
    {
        var originated = 0;
        while (budget-- > 0 && !ct.IsCancellationRequested)
        {
            var target = await _repo.ClaimNextDialableTargetAsync(campaign.Id, _retry.MaxAttempts, ct);
            if (target is null) break;

            // Order: concurrency token first, then rate. If either denies, revert cleanly.
            if (!await _concurrency.TryTakeAsync(campaign.Id, capacityCap, ct))
            {
                await _repo.RevertDialingToPendingAsync(target.Id, ct);
                break; // at cap for this cycle
            }

            if (!await _rate.TryAdmitAsync(campaign, ct))
            {
                await _concurrency.GiveBackAsync(campaign.Id, ct);
                await _repo.RevertDialingToPendingAsync(target.Id, ct);
                break; // burst ceiling hit — move to next campaign
            }

            var fired = await _originator.FireAsync(campaign, target, queueName, ct);
            if (fired) originated++;
            // FireAsync reverts + gives back the token on failure internally.
        }
        return originated;
    }

    private async Task<int> SafeFreeAgentsAsync(CampaignModel c, CancellationToken ct)
    {
        try { return await _agents.GetFreeAgentsForCampaignAsync(c, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "Free-agent lookup failed for {CampaignId}", c.Id); return 0; }
    }
}
