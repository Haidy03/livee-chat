using Microsoft.Extensions.Options;
using Outbound.Event.Campaign.Lookups;
using Outbound.Event.Campaign.Models;
using Outbound.Event.Campaign.Options;
using Outbound.Event.Campaign.Pacing;
using Outbound.Event.Campaign.Persistence;
using Outbound.Infrastructure.Ami;

namespace Outbound.Event.Campaign.Actions;

public interface IOriginator
{
    /// <summary>
    /// Fire an AMI Originate for <paramref name="target"/>. Returns immediately after the socket
    /// write; the outcome arrives asynchronously via AMI events → <c>OutcomeFinalizer</c>.
    /// On failure the caller's concurrency token / target claim is reverted here.
    /// </summary>
    Task<bool> FireAsync(CampaignModel campaign, CampaignTarget target, string queueName, CancellationToken ct);
}

public sealed class AsteriskOriginator : IOriginator
{
    public const string AnsweredContext = "predictive-answered";

    private readonly IAmiActionSender _ami;
    private readonly IAttemptRegistry _registry;
    private readonly ICallAttemptRepository _attempts;
    private readonly ITenantTrunkRepository _trunks;
    private readonly CampaignRepository _campaigns;
    private readonly IConcurrencyCounter _concurrency;
    private readonly AsteriskOptions _opt;
    private readonly ILogger<AsteriskOriginator> _log;

    public AsteriskOriginator(
        IAmiActionSender ami,
        IAttemptRegistry registry,
        ICallAttemptRepository attempts,
        ITenantTrunkRepository trunks,
        CampaignRepository campaigns,
        IConcurrencyCounter concurrency,
        IOptions<AsteriskOptions> opt,
        ILogger<AsteriskOriginator> log)
    {
        _ami = ami;
        _registry = registry;
        _attempts = attempts;
        _trunks = trunks;
        _campaigns = campaigns;
        _concurrency = concurrency;
        _opt = opt.Value;
        _log = log;
    }

    public async Task<bool> FireAsync(CampaignModel campaign, CampaignTarget target, string queueName, CancellationToken ct)
    {
        if (!_ami.IsReady)
        {
            _log.LogWarning("AMI not ready — reverting target {TargetId}.", target.Id);
            await RevertAsync(campaign.Id, target, ct);
            return false;
        }
        
        // var trunk = await _trunks.GetAsync(campaign.TenantId, ct);
        // if (trunk is null)
        // {
        //     _log.LogError("No account row for tenant {Tenant}; failing target {TargetId}.", campaign.TenantId, target.Id);
        //     await _campaigns.FailTargetAsync(target.Id, "no_account", $"No account for tenant {campaign.TenantId}", ct);
        //     await _concurrency.GiveBackAsync(campaign.Id, ct);
        //     return false;
        // }
        var trunk = new { Trunk = "", CallerId = "\"Asterisk Outbound\" <1000>" };
        var attemptId = target.AttemptId ?? Guid.NewGuid().ToString("N");
        var correlationId = $"{campaign.Id}:{target.Id}:{DateTime.UtcNow:yyyyMMddHHmmss}";

        var seed = new CallAttempt
        {
            AttemptId = attemptId,
            TargetId = target.Id,
            CampaignId = campaign.Id,
            TenantId = campaign.TenantId,
            AttemptNumber = target.Attempts + 1,
            StartedAt = DateTime.UtcNow,
            Trunk = trunk.Trunk,
            CorrelationId = correlationId,
        };

        try { await _attempts.StartAsync(seed, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "call_attempts insert failed for {AttemptId} — continuing.", attemptId); }

        _registry.Register(new AttemptContext(
            attemptId, target.Id, campaign.Id, campaign.TenantId, target.Attempts + 1, correlationId));

        var channel = string.IsNullOrWhiteSpace(trunk.Trunk)
            ? $"PJSIP/{target.Phone}"
            : $"PJSIP/{target.Phone}@{trunk.Trunk}";

        var fields = new List<KeyValuePair<string, string>>
        {
            new("Action",   "Originate"),
            new("Channel",  channel),
            new("Context",  AnsweredContext),
            new("Exten",    "s"),
            new("Priority", "1"),
            new("Async",    "true"),
            new("ActionID", attemptId),
            new("Timeout",  (_opt.RingTimeoutSeconds * 1000).ToString()),
            new("CallerID", trunk.CallerId ?? string.Empty),
            new("Variable", $"__CAMPAIGN_ID={campaign.Id}"),
            new("Variable", $"__CONTACT_ID={target.Id}"),
            new("Variable", $"__ATTEMPT_ID={attemptId}"),
            new("Variable", $"__OUTBOUND_QUEUE={queueName}"),
        };

        try
        {
            await _ami.SendAsync(fields, ct);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "AMI originate send failed for target {TargetId}", target.Id);
            _registry.Drop(attemptId);
            try { await _attempts.FinishAsync(attemptId, "originate_send_failed", null, ct); } catch { /* best effort */ }
            await RevertAsync(campaign.Id, target, ct);
            return false;
        }
    }

    private async Task RevertAsync(string campaignId, CampaignTarget target, CancellationToken ct)
    {
        try { await _campaigns.RevertDialingToPendingAsync(target.Id, ct); } catch { /* best effort */ }
        await _concurrency.GiveBackAsync(campaignId, ct);
    }
}
