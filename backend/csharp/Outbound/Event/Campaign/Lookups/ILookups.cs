namespace Outbound.Event.Campaign.Lookups;

public sealed record TenantTrunkInfo(string Trunk, string CallerId);

public sealed record CampaignDialingInfo(string DialingMode, double PowerRatio, string QueueId, string TenantId);

public interface ITenantTrunkRepository
{
    Task<TenantTrunkInfo?> GetAsync(string tenantId, CancellationToken ct);
}

public interface ICampaignLookupRepository
{
    Task<CampaignDialingInfo?> GetAsync(string campaignId, CancellationToken ct);
}
