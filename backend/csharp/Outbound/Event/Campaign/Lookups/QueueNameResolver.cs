using Outbound.Event.Campaign.Models;

namespace Outbound.Event.Campaign.Lookups;

/// <summary>
/// Resolves the Asterisk queue name a campaign dials into. There is exactly one rule and it must
/// match both what the dialplan export creates and what the free-agent tracker looks up:
///   assignedMode == "agents" (default) → t_{tenantId}__qc_{campaignId}
///   assignedMode == "queue"            → campaign.QueueId verbatim
/// A misconfiguration (queue mode with blank QueueId) returns null; callers must skip.
/// </summary>
public static class QueueNameResolver
{
    public static string? Resolve(CampaignModel campaign)
    {
        var mode = string.IsNullOrWhiteSpace(campaign.AssignedMode)
            ? "agents"
            : campaign.AssignedMode.Trim().ToLowerInvariant();

        if (mode == "queue")
            return string.IsNullOrWhiteSpace(campaign.QueueId) ? null : campaign.QueueId;

        // "agents" (and any unknown value) → derived queue.
        return $"t_{campaign.TenantId}__qc_{campaign.Id}";
    }
}
