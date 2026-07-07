using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public sealed class CampaignTargetListFilter
{
    public string? Status { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed class CampaignTargetPage
{
    public IReadOnlyList<CampaignTarget> Items { get; init; } = Array.Empty<CampaignTarget>();
    public long TotalCount { get; init; }
}

public interface ICampaignTargetRepository : IRepository<CampaignTarget>
{
    Task<CampaignTargetPage> ListAsync(string tenantId, string campaignId, CampaignTargetListFilter filter, CancellationToken ct = default);

    Task<CampaignTarget?> GetForCampaignAsync(string tenantId, string campaignId, string targetId, CancellationToken ct = default);

    /// <summary>Bulk insert; chunked by caller. Returns the number of inserted documents.</summary>
    Task<long> InsertManyAsync(IReadOnlyList<CampaignTarget> targets, CancellationToken ct = default);

    /// <summary>
    /// Atomically updates a target's status and lastCallAt. Returns the (previousStatus, newStatus)
    /// so callers can update the parent campaign's counters with `$inc`. Null = target missing.
    /// </summary>
    Task<(string previousStatus, string newStatus)?> UpdateStatusAsync(
        string tenantId, string campaignId, string targetId, string newStatus, string lastCallAtIso, CancellationToken ct = default);

    Task<CampaignTarget?> DeleteForCampaignAsync(string tenantId, string campaignId, string targetId, CancellationToken ct = default);

    /// <summary>Returns counts grouped by status (status -> count) for one campaign.</summary>
    Task<IReadOnlyDictionary<string, long>> CountByStatusAsync(string tenantId, string campaignId, CancellationToken ct = default);

    Task<long> DeleteAllForCampaignAsync(string tenantId, string campaignId, CancellationToken ct = default);
}
