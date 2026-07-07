using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public sealed class CampaignReceivedCallPage
{
    public IReadOnlyList<CampaignReceivedCallItem> Items { get; init; } = Array.Empty<CampaignReceivedCallItem>();
    public long TotalCount { get; init; }
}

public interface ICampaignReceivedCallRepository : IRepository<CampaignReceivedCallItem>
{
    Task<CampaignReceivedCallPage> ListAsync(string tenantId, string campaignId, int page, int pageSize, CancellationToken ct = default);
    Task<long> InsertManyAsync(IReadOnlyList<CampaignReceivedCallItem> items, CancellationToken ct = default);
    Task<long> DeleteAllForCampaignAsync(string tenantId, string campaignId, CancellationToken ct = default);
}
