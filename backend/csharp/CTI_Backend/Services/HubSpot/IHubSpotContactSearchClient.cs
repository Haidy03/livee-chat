using CtiBackend.Models.HubSpot;

namespace CtiBackend.Integrations.HubSpot;
public interface IHubSpotContactSearchClient
{
    Task<HubSpotSearchResponse> SearchByPhoneAsync(
        string tenantId,
        IReadOnlyList<string> phoneVariants,
        int limit,
        CancellationToken ct);
}
