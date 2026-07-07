using System.Diagnostics;
using CTI.Models.HubSpot;
using CtiBackend.Integrations.HubSpot;
using CtiBackend.Integrations.HubSpot.Services;
using CtiBackend.Models.HubSpot;
using CtiBackend.Options;
using Microsoft.Extensions.Options;

namespace CtiBackend.Services.HubSpot;

public sealed class HubSpotCallerLookupService : IHubSpotCallerLookupService
{
    private readonly HubSpotOptions _opt;
    private readonly IHubSpotIntegrationRepository _repo;
    private readonly IPhoneNumberNormalizer _normalizer;
    private readonly IHubSpotContactSearchClient _client;
    private readonly HubSpotLookupCache _cache;
    private readonly ILogger<HubSpotCallerLookupService> _log;

    public HubSpotCallerLookupService(
        IOptions<HubSpotOptions> opt,
        IHubSpotIntegrationRepository repo,
        IPhoneNumberNormalizer normalizer,
        IHubSpotContactSearchClient client,
        HubSpotLookupCache cache,
        ILogger<HubSpotCallerLookupService> log)
    {
        _opt = opt.Value; _repo = repo; _normalizer = normalizer; _client = client;
        _cache = cache; _log = log;
    }

    public async Task<HubSpotCallerLookupResult> FindCallerAsync(
        string tenantId, string callerNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new HubSpotLookupException("INVALID_TENANT", "Tenant id is required.");

        var normalized = _normalizer.TryNormalize(callerNumber)
            ?? throw new HubSpotLookupException("INVALID_CALLER_NUMBER", "A valid caller number is required.");

        var masked = _normalizer.Mask(callerNumber);
        var primaryKey = normalized.NationalSignificantNumber;

        // Cache lookup
        if (_cache.TryGet(tenantId, primaryKey, out var cached) && cached is not null)
        {
            _log.LogDebug("HubSpot caller cache hit Tenant={Tenant} Caller={Caller}", tenantId, masked);
            return cached;
        }

        // Load integration (tenant-scoped)

        var integration = await _repo.GetByTenantAsync(tenantId, ct);
        if (integration is null || integration.Status != HubSpotIntegrationStatus.Connected)
            throw new HubSpotLookupException("HUBSPOT_NOT_CONNECTED", "HubSpot is not connected for this tenant.");

        var required = string.IsNullOrWhiteSpace(_opt.RequiredContactScope)
            ? "crm.objects.contacts.read"
            : _opt.RequiredContactScope;
        if (integration.GrantedScopes is { Count: > 0 } &&
            !integration.GrantedScopes.Contains(required, StringComparer.OrdinalIgnoreCase))
        {
            throw new HubSpotLookupException("HUBSPOT_SCOPE_MISSING",
                $"The HubSpot connection does not grant {required}.");
        }

        var sw = Stopwatch.StartNew();
        HubSpotSearchResponse search;
        try
        {
            search = await _client.SearchByPhoneAsync(tenantId, normalized.SearchVariants,
                _opt.SearchResultLimit > 0 ? _opt.SearchResultLimit : 10, ct);
        }
        catch (HubSpotOAuthException ex)
        {
            // Token provider couldn't get/refresh a token.
            var code = ex.Code switch
            {
                "HUBSPOT_AUTH_REVOKED" => "HUBSPOT_REAUTHORIZATION_REQUIRED",
                "HUBSPOT_NOT_CONNECTED" => "HUBSPOT_NOT_CONNECTED",
                _ => "HUBSPOT_REQUEST_FAILED",
            };
            throw new HubSpotLookupException(code, ex.Message);
        }
        sw.Stop();

        var contacts = search.Results.Select(Map).ToList();
        var result = new HubSpotCallerLookupResult
        {
            Found = contacts.Count > 0,
            HasMultipleMatches = contacts.Count > 1,
            NormalizedCallerNumber = primaryKey,
            TotalMatches = contacts.Count,
            PrimaryContact = SelectPrimary(contacts, normalized),
            Contacts = contacts,
        };

        var ttlSeconds = _opt.CallerLookupCacheSeconds;
        if (ttlSeconds > 0)
        {
            var ttl = TimeSpan.FromSeconds(result.Found ? ttlSeconds : Math.Max(5, ttlSeconds / 2));
            _cache.Set(tenantId, primaryKey, result, ttl);
        }

        _log.LogInformation(
            "HubSpot caller lookup completed. TenantId={Tenant} HubId={HubId} Caller={Caller} Matches={Matches} ElapsedMs={Ms}",
            tenantId, integration.HubSpotAccountId, masked, result.TotalMatches, sw.ElapsedMilliseconds);

        return result;
    }

    private static HubSpotCallerContact? SelectPrimary(List<HubSpotCallerContact> contacts, NormalizedPhoneNumber n)
    {
        if (contacts.Count == 0) return null;
        if (contacts.Count == 1) return contacts[0];

        bool DigitsMatch(string? v) => !string.IsNullOrWhiteSpace(v) && (
            v == n.E164 ||
            v == n.NationalSignificantNumber ||
            new string(v.Where(char.IsDigit).ToArray()).EndsWith(n.NationalSignificantNumber, StringComparison.Ordinal));

        return contacts.FirstOrDefault(c => DigitsMatch(c.Phone) || DigitsMatch(c.MobilePhone));
    }

    private static HubSpotCallerContact Map(HubSpotSearchResult r)
    {
        string? Get(string key) => r.Properties.TryGetValue(key, out var v) ? v : null;
        var first = Get("firstname");
        var last = Get("lastname");
        var full = string.Join(' ', new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s))!);
        return new HubSpotCallerContact
        {
            HubSpotContactId = r.Id,
            FirstName = first,
            LastName = last,
            FullName = string.IsNullOrWhiteSpace(full) ? null : full,
            Email = Get("email"),
            Phone = Get("phone"),
            MobilePhone = Get("mobilephone"),
            Company = Get("company"),
            JobTitle = Get("jobtitle"),
            LifecycleStage = Get("lifecyclestage"),
            HubSpotOwnerId = Get("hubspot_owner_id"),
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
        };
    }
}
