using CTI.Models.HubSpot;
using CtiBackend.Models.Responses;
using CtiBackend.Services.HubSpot;
using CtiBackend.Tenant;
using Microsoft.AspNetCore.Mvc;

namespace CtiBackend.Integrations.HubSpot.Controllers;

[ApiController]
[Route("api/integrations/hubspot/contacts")]
public sealed class HubSpotContactsController : ControllerBase
{
    private readonly IHubSpotCallerLookupService _lookup;
    private readonly ITenantContext _tenant;
    private readonly ILogger<HubSpotContactsController> _log;

    public HubSpotContactsController(
        IHubSpotCallerLookupService lookup,
        ITenantContext tenant,
        ILogger<HubSpotContactsController> log)
    {
        _lookup = lookup; _tenant = tenant; _log = log;
    }

    public sealed class SearchByPhoneRequest
    {
        public string? CallerNumber { get; set; }
    }

    [HttpPost("search-by-phone")]
    public async Task<ActionResult<ApiResponse<HubSpotCallerLookupResult>>> SearchByPhone(
        [FromBody] SearchByPhoneRequest body, CancellationToken ct)
    {
        if (!_tenant.IsAuthenticated)
            return Unauthorized(ApiResponse<HubSpotCallerLookupResult>.Fail("UNAUTHORIZED"));

        if (body is null || string.IsNullOrWhiteSpace(body.CallerNumber))
            return BadRequest(ApiResponse<HubSpotCallerLookupResult>.Fail("INVALID_CALLER_NUMBER"));

        try
        {
            var result = await _lookup.FindCallerAsync(_tenant.TenantId!, body.CallerNumber, ct);
            return Ok(ApiResponse<HubSpotCallerLookupResult>.Ok(result));
        }
        catch (HubSpotLookupException ex)
        {
            _log.LogWarning("HubSpot caller lookup error Code={Code} Status={Status}", ex.Code, ex.HttpStatus);
            return ex.Code switch
            {
                "INVALID_CALLER_NUMBER" => BadRequest(ApiResponse<HubSpotCallerLookupResult>.Fail(ex.Code)),
                "HUBSPOT_NOT_CONNECTED" => StatusCode(409, ApiResponse<HubSpotCallerLookupResult>.Fail(ex.Code)),
                "HUBSPOT_SCOPE_MISSING" => StatusCode(403, ApiResponse<HubSpotCallerLookupResult>.Fail(ex.Code)),
                "HUBSPOT_REAUTHORIZATION_REQUIRED" => StatusCode(401, ApiResponse<HubSpotCallerLookupResult>.Fail(ex.Code)),
                "HUBSPOT_RATE_LIMITED" => StatusCode(429, ApiResponse<HubSpotCallerLookupResult>.Fail(ex.Code)),
                _ => StatusCode(502, ApiResponse<HubSpotCallerLookupResult>.Fail(ex.Code)),
            };
        }
    }
}
