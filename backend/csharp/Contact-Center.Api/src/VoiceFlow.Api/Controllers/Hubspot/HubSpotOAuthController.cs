using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoiceFlow.Application.Common;
using VoiceFlow.Application.Interfaces.Hubspot;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Hubspot;
using VoiceFlow.Core.Exceptions.Hubspot;
using VoiceFlow.Infrastructure.ExternalServices.Hubspot;

namespace VoiceFlow.Api.Controllers.Hubspot;

[ApiController]
public sealed class HubSpotOAuthController : ControllerBase
{
    private readonly IHubSpotOAuthService _service;
    private readonly IHubSpotTokenProvider _tokens;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<HubSpotOAuthController> _log;

    public HubSpotOAuthController(IHubSpotOAuthService service,
                                  IHubSpotTokenProvider tokens,
                                  ITenantContext tenant,
                                  ICurrentUser currentUser,
                                  ILogger<HubSpotOAuthController> log)
    {
        _service = service; _tokens = tokens; _tenant = tenant; _log = log; _currentUser = currentUser;
    }

    // -------- Status --------
    [HttpGet("/api/integrations/hubspot/status")]
    public async Task<ActionResult<ApiResponse<HubSpotIntegrationStatusResponse>>> Status(CancellationToken ct)
    {
        /*if (!_tenant.is)
            return Unauthorized(ApiResponse<HubSpotIntegrationStatusResponse>.Fail("unauthorized"));
        */
        var status = await _service.GetStatusAsync(_tenant.TenantId!, ct);
        return Ok(ApiResponse<HubSpotIntegrationStatusResponse>.Ok(status));
    }

    // -------- Start OAuth --------
    [HttpGet("/api/integrations/hubspot/connect")]
    //[RequireTenantAdmin]
    public async Task<IActionResult> Connect([FromQuery] string? returnPath, CancellationToken ct)
    {
        var url = await _service.BuildAuthorizationUrlAsync(_tenant.TenantId!, _currentUser.UserId!, returnPath, ct);
        return Ok(ApiResponse<object>.Ok(new { url }));
    }

    // -------- Callback --------
    [HttpGet("/integrations/hub_spot/oauth_redirect")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _log.LogWarning("HubSpot callback returned error: {Error}", error);
            return Redirect(_service.FailureRedirectUrl($"error: {error}, code: {code}, state: {state}"));
        }
        if (string.IsNullOrWhiteSpace(state))
            return Redirect(_service.FailureRedirectUrl($"error: {error}, code: {code}, state: {state}"));
        if (string.IsNullOrWhiteSpace(code))
            return Redirect(_service.FailureRedirectUrl($"error: {error}, code: {code}, state: {state}"));

        try
        {
            await _service.HandleCallbackAsync(code, state, ct);
            return Redirect(_service.SuccessRedirectUrl());
        }
        catch (HubSpotOAuthException ex)
        {
            _log.LogWarning("HubSpot callback failed: {Code}", ex.Code);
            return Redirect(_service.FailureRedirectUrl($"error: {error}, code: {code}, state: {state}"));
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unexpected HubSpot callback error");
            return Redirect(_service.FailureRedirectUrl($"error: {error}, code: {code}, state: {state}"));
        }
    }

    // -------- Disconnect --------
    [HttpPost("/api/integrations/hubspot/disconnect")]
    //[RequireTenantAdmin]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        await _service.DisconnectAsync(_tenant.TenantId!, ct);
        return Ok(new { success = true, connected = false });
    }

    // -------- Test --------
    [HttpPost("/api/integrations/hubspot/test")]
    //[RequireTenantAdmin]
    public async Task<IActionResult> Test(CancellationToken ct)
    {
        try
        {
            var token = await _tokens.GetValidAccessTokenAsync(_tenant.TenantId!, ct);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var resp = await client.GetAsync("https://api.hubapi.com/account-info/v3/details", ct);
            return Ok(new { success = resp.IsSuccessStatusCode, status = (int)resp.StatusCode });
        }
        catch (HubSpotOAuthException ex)
        {
            return BadRequest(new { success = false, errors = new[] { ex.Code } });
        }
    }
}
