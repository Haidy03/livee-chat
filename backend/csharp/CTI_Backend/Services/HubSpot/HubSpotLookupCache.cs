using CTI.Models.HubSpot;
using Microsoft.Extensions.Caching.Memory;

namespace CtiBackend.Integrations.HubSpot.Services;

public sealed class HubSpotLookupCache
{
    private readonly IMemoryCache _cache;

    public HubSpotLookupCache(IMemoryCache cache) { _cache = cache; }

    private static string Key(string tenantId, string normalized) => $"hubspot-caller:{tenantId}:{normalized}";

    public bool TryGet(string tenantId, string normalized, out HubSpotCallerLookupResult? result)
        => _cache.TryGetValue(Key(tenantId, normalized), out result);

    public void Set(string tenantId, string normalized, HubSpotCallerLookupResult result, TimeSpan ttl)
    {
        if (ttl <= TimeSpan.Zero) return;
        _cache.Set(Key(tenantId, normalized), result, ttl);
    }
}
