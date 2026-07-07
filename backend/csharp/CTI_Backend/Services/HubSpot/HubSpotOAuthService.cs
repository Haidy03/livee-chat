using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CtiBackend.Models.HubSpot;
using CtiBackend.Options;
using CtiBackend.Services.Security;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace CtiBackend.Services.HubSpot;

public sealed class HubSpotOAuthException : Exception
{
    public string Code { get; }
    public HubSpotOAuthException(string code, string message) : base(message) { Code = code; }
}

public interface IHubSpotOAuthService
{
    Task<string> BuildAuthorizationUrlAsync(string tenantId, string userId, string? returnPath, CancellationToken ct);
    Task<HubSpotIntegration> HandleCallbackAsync(string code, string state, CancellationToken ct);
    Task DisconnectAsync(string tenantId, CancellationToken ct);
    Task<HubSpotIntegrationStatusResponse> GetStatusAsync(string tenantId, CancellationToken ct);
    string SuccessRedirectUrl();
    string FailureRedirectUrl(string code);
}

public sealed class HubSpotOAuthService : IHubSpotOAuthService
{
    private readonly HubSpotOptions _opt;
    private readonly IHubSpotIntegrationRepository _repo;
    private readonly ITokenProtector _protector;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<HubSpotOAuthService> _log;

    public HubSpotOAuthService(IOptions<HubSpotOptions> opt,
                               IHubSpotIntegrationRepository repo,
                               ITokenProtector protector,
                               IHttpClientFactory http,
                               ILogger<HubSpotOAuthService> log)
    {
        _opt = opt.Value; _repo = repo; _protector = protector; _http = http; _log = log;
    }

    // ---- State helpers ----
    private static string NewState()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlTextEncoder.Encode(bytes);
    }
    private static string Hash(string s)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes);
    }

    public async Task<string> BuildAuthorizationUrlAsync(string tenantId, string userId, string? returnPath, CancellationToken ct)
    {
        var raw = NewState();
        var hash = Hash(raw);
        var now = DateTime.UtcNow;
        var record = new Models.HubSpot.HubSpotOAuthState
        {
            StateHash = hash,
            TenantId = tenantId,
            UserId = userId,
            ReturnPath = returnPath,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddMinutes(_opt.StateExpirationMinutes),
        };
        await _repo.StoreStateAsync(record, ct);

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _opt.ClientId,
            ["redirect_uri"] = _opt.RedirectUri,
            ["scope"] = string.Join(' ', _opt.Scopes),
            ["state"] = raw,
        };
        return QueryHelpers.AddQueryString(_opt.AuthorizationUrl, query);
    }

    public async Task<HubSpotIntegration> HandleCallbackAsync(string code, string state, CancellationToken ct)
    {
        var consumed = await _repo.ConsumeStateAsync(Hash(state), ct);
        if (consumed is null)
            throw new HubSpotOAuthException("invalid_state", "OAuth state was missing, expired, or already used.");

        var tokens = await ExchangeCodeAsync(code, ct);

        var existing = await _repo.GetByTenantAsync(consumed.TenantId, ct);
        var integration = existing ?? new HubSpotIntegration { TenantId = consumed.TenantId };

        // Look up account metadata (best effort).
        HubSpotTokenAccountInfo? accountInfo = null;
        try { accountInfo = await GetAccountInfoAsync(tokens.AccessToken, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "Failed to fetch HubSpot account info for tenant {Tenant}", consumed.TenantId); }

        integration.HubSpotAccountId = accountInfo?.HubId?.ToString();
        integration.HubSpotAccountName = accountInfo?.HubDomain;
        integration.EncryptedAccessToken = _protector.ProtectAccessToken(tokens.AccessToken);
        if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
            integration.EncryptedRefreshToken = _protector.ProtectRefreshToken(tokens.RefreshToken);
        integration.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokens.ExpiresIn - _opt.AccessTokenSafetyMarginSeconds);
        integration.GrantedScopes = accountInfo?.Scopes ?? new List<string>(_opt.Scopes);
        integration.Status = HubSpotIntegrationStatus.Connected;
        integration.ConnectedByUserId = consumed.UserId;
        integration.ConnectedAtUtc = existing?.ConnectedAtUtc ?? DateTime.UtcNow;
        integration.LastRefreshedAtUtc = DateTime.UtcNow;
        integration.DisconnectedAtUtc = null;
        integration.LastErrorCode = null;
        integration.LastErrorAtUtc = null;

        await _repo.UpsertIntegrationAsync(integration, ct);
        _log.LogInformation("HubSpot integration connected for tenant {Tenant} by user {User}",
            consumed.TenantId, consumed.UserId);
        return integration;
    }

    public async Task DisconnectAsync(string tenantId, CancellationToken ct)
    {
        var existing = await _repo.GetByTenantAsync(tenantId, ct);
        if (existing is null) return;

        // Best-effort revoke
        if (!string.IsNullOrWhiteSpace(existing.EncryptedRefreshToken))
        {
            try
            {
                var rt = _protector.UnprotectRefreshToken(existing.EncryptedRefreshToken);
                var client = _http.CreateClient("hubspot");
                await client.DeleteAsync($"https://api.hubapi.com/oauth/v1/refresh-tokens/{Uri.EscapeDataString(rt)}", ct);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "HubSpot revoke failed for tenant {Tenant} (continuing)", tenantId);
            }
        }

        existing.EncryptedAccessToken = null;
        existing.EncryptedRefreshToken = null;
        existing.AccessTokenExpiresAtUtc = null;
        existing.Status = HubSpotIntegrationStatus.Disconnected;
        existing.DisconnectedAtUtc = DateTime.UtcNow;
        await _repo.UpsertIntegrationAsync(existing, ct);
        _log.LogInformation("HubSpot integration disconnected for tenant {Tenant}", tenantId);
    }

    public async Task<HubSpotIntegrationStatusResponse> GetStatusAsync(string tenantId, CancellationToken ct)
    {
        var i = await _repo.GetByTenantAsync(tenantId, ct);
        if (i is null)
            return new HubSpotIntegrationStatusResponse { Connected = false, Status = nameof(HubSpotIntegrationStatus.Disconnected) };
        return new HubSpotIntegrationStatusResponse
        {
            Connected = i.Status == HubSpotIntegrationStatus.Connected,
            Status = i.Status.ToString(),
            AccountId = i.HubSpotAccountId,
            AccountName = i.HubSpotAccountName,
            GrantedScopes = i.GrantedScopes,
            ConnectedAtUtc = i.ConnectedAtUtc,
            ConnectedByUserId = i.ConnectedByUserId,
            LastRefreshedAtUtc = i.LastRefreshedAtUtc,
            LastErrorCode = i.LastErrorCode,
        };
    }

    public string SuccessRedirectUrl() => _opt.FrontendSuccessUrl;
    public string FailureRedirectUrl(string code) =>
        QueryHelpers.AddQueryString(_opt.FrontendFailureUrl, new Dictionary<string, string?> { ["reason"] = code });

    private async Task<HubSpotTokenResponse> ExchangeCodeAsync(string code, CancellationToken ct)
    {
        var client = _http.CreateClient("hubspot");
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _opt.ClientId,
            ["client_secret"] = _opt.ClientSecret,
            ["redirect_uri"] = _opt.RedirectUri,
            ["code"] = code,
        });
        using var resp = await client.PostAsync(_opt.TokenUrl, form, ct);
        if (!resp.IsSuccessStatusCode)
        {
            _log.LogWarning("HubSpot token exchange failed: status={Status}", (int)resp.StatusCode);
            throw new HubSpotOAuthException("token_exchange_failed", "HubSpot rejected the authorization code.");
        }
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<HubSpotTokenResponse>(json)
               ?? throw new HubSpotOAuthException("token_exchange_failed", "Empty token response.");
    }

    private async Task<HubSpotTokenAccountInfo?> GetAccountInfoAsync(string accessToken, CancellationToken ct)
    {
        var client = _http.CreateClient("hubspot");
        var req = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.hubapi.com/oauth/v1/access-tokens/{Uri.EscapeDataString(accessToken)}");
        using var resp = await client.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<HubSpotTokenAccountInfo>(json);
    }
}
