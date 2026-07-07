using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface ICampaignRepository : IRepository<Campaign>
{
    Task<IEnumerable<Campaign>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Campaign?> GetByIdForTenantAsync(string id, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically applies counter deltas (e.g. {pending: -1, called: +1}) and bumps version/lastActivityAt
    /// without rewriting unrelated fields. Returns the updated Campaign or null if not found.
    /// Status keys: "pending" | "called" | "successful" | "failed" | "callback" | "total".
    /// </summary>
    Task<Campaign?> ApplyTargetCounterDeltasAsync(
        string id,
        string tenantId,
        IReadOnlyDictionary<string, long> deltas,
        CancellationToken cancellationToken = default);
}
