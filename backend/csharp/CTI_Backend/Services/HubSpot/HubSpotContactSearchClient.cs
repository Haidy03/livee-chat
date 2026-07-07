using System.Net;
using System.Text.Json;
using CTI.Models.HubSpot;
using CtiBackend.Models.HubSpot;
using CtiBackend.Services.HubSpot;

namespace CtiBackend.Integrations.HubSpot.Services;

public sealed class HubSpotContactSearchClient : IHubSpotContactSearchClient
{
    private const int HubSpotMaxFilterGroups = 5;
    private static readonly string[] DefaultProperties =
    {
        "firstname","lastname","email","phone","mobilephone",
        "company","jobtitle","lifecyclestage","hubspot_owner_id",
        "createdate","lastmodifieddate"
    };

    private readonly HubSpotApiClient _api;
    private readonly ILogger<HubSpotContactSearchClient> _log;

    public HubSpotContactSearchClient(HubSpotApiClient api, ILogger<HubSpotContactSearchClient> log)
    {
        _api = api; _log = log;
    }

    public async Task<HubSpotSearchResponse> SearchByPhoneAsync(
        string tenantId,
        IReadOnlyList<string> phoneVariants,
        int limit,
        CancellationToken ct)
    {
        if (phoneVariants is null || phoneVariants.Count == 0)
            return new HubSpotSearchResponse();

        // Build filterGroups: one per (property, variant). Cap to HubSpot's 5 group limit.
        var groups = new List<HubSpotFilterGroup>(HubSpotMaxFilterGroups);
        foreach (var v in phoneVariants)
        {
            foreach (var prop in new[] { "phone", "mobilephone" })
            {
                if (groups.Count >= HubSpotMaxFilterGroups) break;
                groups.Add(new HubSpotFilterGroup
                {
                    Filters = new[] { new HubSpotFilter { PropertyName = prop, Operator = "EQ", Value = v } }
                });
            }
            if (groups.Count >= HubSpotMaxFilterGroups) break;
        }

        var payload = new HubSpotSearchRequest
        {
            FilterGroups = groups,
            Properties = DefaultProperties,
            Limit = Math.Clamp(limit, 1, 100),
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.hubapi.com/crm/v3/objects/contacts/search")
        {
            Content = JsonContent.Create(payload)
        };

        using var resp = await _api.SendAsync(tenantId, req, ct);
        var status = (int)resp.StatusCode;
        var correlation = resp.Headers.TryGetValues("X-HubSpot-Correlation-Id", out var c) ? string.Join(',', c) : null;

        if (resp.IsSuccessStatusCode)
        {
            var stream = await resp.Content.ReadAsStreamAsync();
            var data = await JsonSerializer.DeserializeAsync<HubSpotSearchResponse>(stream)
                       ?? new HubSpotSearchResponse();
            _log.LogInformation("HubSpot search OK Tenant={Tenant} Status={Status} Total={Total} Corr={Corr}",
                tenantId, status, data.Total, correlation);
            return data;
        }

        // Drain a short body for logging without leaking sensitive info
        _log.LogWarning("HubSpot search failed Tenant={Tenant} Status={Status} Corr={Corr}", tenantId, status, correlation);

        throw resp.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new HubSpotLookupException("HUBSPOT_REAUTHORIZATION_REQUIRED",
                "HubSpot connection is no longer authorized. Reconnect HubSpot.", status),
            HttpStatusCode.Forbidden => new HubSpotLookupException("HUBSPOT_SCOPE_MISSING",
                "HubSpot connection does not grant the required scope.", status),
            HttpStatusCode.NotFound => new HubSpotLookupException("HUBSPOT_REQUEST_FAILED",
                "HubSpot endpoint not found.", status),
            (HttpStatusCode)429 => new HubSpotLookupException("HUBSPOT_RATE_LIMITED",
                "HubSpot rate limit exceeded.", status),
            _ => new HubSpotLookupException("HUBSPOT_REQUEST_FAILED",
                $"HubSpot search failed with status {status}.", status),
        };
    }
}
