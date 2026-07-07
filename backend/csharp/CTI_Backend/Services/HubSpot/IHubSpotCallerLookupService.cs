using CTI.Models.HubSpot;

namespace CtiBackend.Services.HubSpot;

public interface IHubSpotCallerLookupService
{
    Task<HubSpotCallerLookupResult> FindCallerAsync(
        string tenantId, string callerNumber, CancellationToken ct = default);
}
