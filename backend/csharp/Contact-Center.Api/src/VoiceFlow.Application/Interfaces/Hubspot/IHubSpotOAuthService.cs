using VoiceFlow.Contracts.Hubspot;
using VoiceFlow.Core.Entities.HubSpot;

namespace VoiceFlow.Application.Interfaces.Hubspot
{
    public interface IHubSpotOAuthService
    {
        Task<string> BuildAuthorizationUrlAsync(string tenantId, string userId, string? returnPath, CancellationToken ct);
        Task<HubSpotIntegration> HandleCallbackAsync(string code, string state, CancellationToken ct);
        Task DisconnectAsync(string tenantId, CancellationToken ct);
        Task<HubSpotIntegrationStatusResponse> GetStatusAsync(string tenantId, CancellationToken ct);
        string SuccessRedirectUrl();
        string FailureRedirectUrl(string code);
    }
}
