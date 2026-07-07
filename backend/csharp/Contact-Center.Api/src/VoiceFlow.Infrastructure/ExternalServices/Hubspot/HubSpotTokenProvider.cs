using System.Text.Json;
using CtiBackend.Services.HubSpot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceFlow.Application.Options;
using VoiceFlow.Contracts.Hubspot;
using VoiceFlow.Core.Entities.HubSpot;
using VoiceFlow.Core.Enums.Hubspot;
using VoiceFlow.Core.Exceptions.Hubspot;
using VoiceFlow.Core.Interfaces.Repositories.Hubspot;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Infrastructure.Auth;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Infrastructure.ExternalServices.Hubspot;

public interface IHubSpotTokenProvider
{
    Task<string> GetValidAccessTokenAsync(string tenantId, CancellationToken ct);
    Task InvalidateAsync(string tenantId, CancellationToken ct);
}

public sealed class HubSpotTokenProvider : IHubSpotTokenProvider
{
    private readonly HubSpotOptions _opt;
    private readonly IHubSpotIntegrationRepository _repo;
    private readonly ITokenProtector _protector;
    private readonly IRefreshLock _lock;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<HubSpotTokenProvider> _log;

    public HubSpotTokenProvider(IOptions<HubSpotOptions> opt,
                                IHubSpotIntegrationRepository repo,
                                ITokenProtector protector,
                                IRefreshLock @lock,
                                IHttpClientFactory http,
                                ILogger<HubSpotTokenProvider> log)
    {
        _opt = opt.Value; _repo = repo; _protector = protector; _lock = @lock; _http = http; _log = log;
    }

    public async Task<string> GetValidAccessTokenAsync(string tenantId, CancellationToken ct)
    {
        var i = await _repo.GetByTenantAsync(tenantId, ct);
        if (i is null || i.Status != HubSpotIntegrationStatus.Connected || string.IsNullOrEmpty(i.EncryptedAccessToken))
            throw new HubSpotOAuthException("HUBSPOT_NOT_CONNECTED", "HubSpot is not connected for this tenant.");

        if (i.AccessTokenExpiresAtUtc is { } exp && exp - DateTime.UtcNow > TimeSpan.FromMinutes(2))
            return _protector.UnprotectAccessToken(i.EncryptedAccessToken);

        await using var releaser = await _lock.AcquireAsync(tenantId, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(15), ct)
            ?? throw new HubSpotOAuthException("HUBSPOT_REFRESH_BUSY", "Could not acquire refresh lock.");

        // Reload after lock
        i = await _repo.GetByTenantAsync(tenantId, ct)
            ?? throw new HubSpotOAuthException("HUBSPOT_NOT_CONNECTED", "Integration vanished during refresh.");
        if (i.AccessTokenExpiresAtUtc is { } exp2 && exp2 - DateTime.UtcNow > TimeSpan.FromMinutes(2))
            return _protector.UnprotectAccessToken(i.EncryptedAccessToken!);

        return await RefreshAsync(i, ct);
    }

    public async Task InvalidateAsync(string tenantId, CancellationToken ct)
    {
        var i = await _repo.GetByTenantAsync(tenantId, ct);
        if (i is null) return;
        i.AccessTokenExpiresAtUtc = DateTime.UtcNow;
        await _repo.UpsertIntegrationAsync(i, ct);
    }

    private async Task<string> RefreshAsync(HubSpotIntegration i, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(i.EncryptedRefreshToken))
            throw new HubSpotOAuthException("HUBSPOT_AUTH_REVOKED", "No refresh token available.");

        var refresh = _protector.UnprotectRefreshToken(i.EncryptedRefreshToken);
        var client = _http.CreateClient("hubspot");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _opt.ClientId,
            ["client_secret"] = _opt.ClientSecret,
            ["refresh_token"] = refresh,
        });
        using var resp = await client.PostAsync(_opt.TokenUrl, form, ct);
        if ((int)resp.StatusCode is 400 or 401 or 403)
        {
            _log.LogWarning("HubSpot refresh rejected for tenant {Tenant}: status={Status}", i.TenantId, (int)resp.StatusCode);
            i.Status = HubSpotIntegrationStatus.Revoked;
            i.EncryptedAccessToken = null;
            i.EncryptedRefreshToken = null;
            i.AccessTokenExpiresAtUtc = null;
            i.LastErrorCode = "HUBSPOT_AUTH_REVOKED";
            i.LastErrorAtUtc = DateTime.UtcNow;
            await _repo.UpsertIntegrationAsync(i, ct);
            throw new HubSpotOAuthException("HUBSPOT_AUTH_REVOKED", "HubSpot rejected the refresh token. Reconnect required.");
        }
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("HubSpot refresh failed for tenant {Tenant}: status={Status}", i.TenantId, (int)resp.StatusCode);
            throw new HubSpotOAuthException("HUBSPOT_TOKEN_REFRESH_FAILED", "Token refresh failed.");
        }

        var tokens = JsonSerializer.Deserialize<HubSpotTokenResponse>(await resp.Content.ReadAsStringAsync(ct))
                     ?? throw new HubSpotOAuthException("HUBSPOT_TOKEN_REFRESH_FAILED", "Empty refresh response.");

        i.EncryptedAccessToken = _protector.ProtectAccessToken(tokens.AccessToken);
        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
            i.EncryptedRefreshToken = _protector.ProtectRefreshToken(tokens.RefreshToken);
        i.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn - _opt.AccessTokenSafetyMarginSeconds);
        i.LastRefreshedAtUtc = DateTime.UtcNow;
        i.Status = HubSpotIntegrationStatus.Connected;
        await _repo.UpsertIntegrationAsync(i, ct);
        return tokens.AccessToken;
    }
}
