using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public sealed class CampaignActivityPage
{
    public IReadOnlyList<CampaignActivityItem> Items { get; init; } = Array.Empty<CampaignActivityItem>();
    public long TotalCount { get; init; }
}

public interface ICampaignActivityRepository : IRepository<CampaignActivityItem>
{
    Task<CampaignActivityPage> ListAsync(string tenantId, string campaignId, int page, int pageSize, CancellationToken ct = default);
    Task<long> InsertManyAsync(IReadOnlyList<CampaignActivityItem> items, CancellationToken ct = default);
    Task<long> DeleteAllForCampaignAsync(string tenantId, string campaignId, CancellationToken ct = default);
}
