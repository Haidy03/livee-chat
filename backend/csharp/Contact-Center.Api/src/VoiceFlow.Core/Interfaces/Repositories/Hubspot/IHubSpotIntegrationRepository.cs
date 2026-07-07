
using VoiceFlow.Core.Entities.HubSpot;

namespace VoiceFlow.Core.Interfaces.Repositories.Hubspot
{
    public interface IHubSpotIntegrationRepository
    {
        Task EnsureIndexesAsync(CancellationToken ct);

        Task StoreStateAsync(HubSpotOAuthState state, CancellationToken ct);
        Task<HubSpotOAuthState?> ConsumeStateAsync(string stateHash, CancellationToken ct);
        Task<long> DeleteExpiredOrConsumedStatesAsync(DateTime olderThanUtc, CancellationToken ct);

        Task<HubSpotIntegration?> GetByTenantAsync(string tenantId, CancellationToken ct);
        Task UpsertIntegrationAsync(HubSpotIntegration integration, CancellationToken ct);
        Task<bool> TryUpdateTokensAsync(HubSpotIntegration integration, long expectedVersion, CancellationToken ct);
    }
}
