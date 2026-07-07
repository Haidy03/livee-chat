using CTI.Models.Directory;
using CtiBackend.Integrations.HubSpot.Services;
using CtiBackend.Options;
using CtiBackend.Services.HubSpot;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CtiBackend.Services.Directory;

public sealed class ContactDirectoryService : IContactDirectoryService
{
    private readonly IMongoCollection<ContactDocument> _collection;
    private readonly IPhoneNumberNormalizer _normalizer;
    private readonly ILogger<ContactDirectoryService> _log;

    public ContactDirectoryService(
        IMongoClient client,
        IOptions<MongoOptions> options,
        IPhoneNumberNormalizer normalizer,
        ILogger<ContactDirectoryService> log)
    {
        var opts = options.Value;
        _collection = client.GetDatabase(opts.Database)
            .GetCollection<ContactDocument>(opts.ContactsCollection);
        _normalizer = normalizer;
        _log = log;
    }

    // Test-only seam.
    internal ContactDirectoryService(
        IMongoCollection<ContactDocument> collection,
        IPhoneNumberNormalizer normalizer,
        ILogger<ContactDirectoryService> log)
    {
        _collection = collection;
        _normalizer = normalizer;
        _log = log;
    }

    public async Task<ContactDocument?> FindByPhoneAsync(string? tenantId, string phone, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var normalized = _normalizer.TryNormalize(phone);
        var variants = normalized?.SearchVariants is { Count: > 0 } v
            ? new List<string>(v)
            : new List<string> { phone.Trim() };

        if (!variants.Contains(phone.Trim())) variants.Add(phone.Trim());

        var b = Builders<ContactDocument>.Filter;
        var phoneFilter = b.Or(
            b.In(x => x.Phone, variants));

        try
        {
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                // Prefer tenant-matched rows; fall back to un-tenanted legacy docs.
                var tenantMatch = await _collection
                    .Find(b.And(phoneFilter, b.Eq(x => x.TenantId, tenantId)))
                    .Limit(1)
                    .FirstOrDefaultAsync(ct);
                if (tenantMatch != null) return tenantMatch;

                var legacy = await _collection
                    .Find(b.And(phoneFilter, b.Or(
                        b.Eq(x => x.TenantId, null),
                        b.Exists(x => x.TenantId, false))))
                    .Limit(1)
                    .FirstOrDefaultAsync(ct);
                return legacy;
            }

            return await _collection.Find(phoneFilter).Limit(1).FirstOrDefaultAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Contact directory lookup failed for {Phone}", _normalizer.Mask(phone));
            return null;
        }
    }
}
