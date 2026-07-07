using CTI.Models.HubSpot;
using CtiBackend.Models.HubSpot;
using CtiBackend.Options;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;

namespace CtiBackend.Services.HubSpot;

public interface IHubSpotIntegrationRepository
{
    Task EnsureIndexesAsync(CancellationToken ct);

    Task StoreStateAsync(HubSpotOAuthState state, CancellationToken ct);
    Task<HubSpotOAuthState?> ConsumeStateAsync(string stateHash, CancellationToken ct);
    Task<long> DeleteExpiredOrConsumedStatesAsync(DateTime olderThanUtc, CancellationToken ct);

    Task<HubSpotIntegration?> GetByTenantAsync(string tenantId, CancellationToken ct);
    Task UpsertIntegrationAsync(HubSpotIntegration integration, CancellationToken ct =default);
    Task<bool> TryUpdateTokensAsync(HubSpotIntegration integration, long expectedVersion, CancellationToken ct);
}

public sealed class MongoHubSpotIntegrationRepository : IHubSpotIntegrationRepository
{
    private readonly IMongoCollection<HubSpotOAuthState> _states;
    private readonly IMongoCollection<HubSpotIntegration> _integrations;

    public MongoHubSpotIntegrationRepository(IMongoClient client,
                                             IOptions<MongoOptions> mongo,
                                             IOptions<HubSpotOptions> hubspot)
    {
        var db = client.GetDatabase(mongo.Value.Database);
        _states = db.GetCollection<HubSpotOAuthState>(hubspot.Value.MongoStatesCollection);
        _integrations = db.GetCollection<HubSpotIntegration>(hubspot.Value.MongoIntegrationsCollection);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct)
    {
        await _states.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<HubSpotOAuthState>(
                Builders<HubSpotOAuthState>.IndexKeys.Ascending(x => x.StateHash),
                new CreateIndexOptions { Unique = true, Name = "ux_state_hash" }),
            new CreateIndexModel<HubSpotOAuthState>(
                Builders<HubSpotOAuthState>.IndexKeys.Ascending(x => x.ExpiresAtUtc),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero, Name = "ttl_expires_at" }),
        }, ct);

        await _integrations.Indexes.CreateOneAsync(
            new CreateIndexModel<HubSpotIntegration>(
                Builders<HubSpotIntegration>.IndexKeys.Ascending(x => x.TenantId),
                new CreateIndexOptions { Unique = true, Name = "ux_tenant_id" }),
            cancellationToken: ct);
    }

    public Task StoreStateAsync(HubSpotOAuthState state, CancellationToken ct)
        => _states.InsertOneAsync(state, cancellationToken: ct);

    public async Task<HubSpotOAuthState?> ConsumeStateAsync(string stateHash, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<HubSpotOAuthState>.Filter.And(
            Builders<HubSpotOAuthState>.Filter.Eq(x => x.StateHash, stateHash),
            Builders<HubSpotOAuthState>.Filter.Eq(x => x.UsedAtUtc, null),
            Builders<HubSpotOAuthState>.Filter.Gt(x => x.ExpiresAtUtc, now));
        var update = Builders<HubSpotOAuthState>.Update.Set(x => x.UsedAtUtc, now);
        return await _states.FindOneAndUpdateAsync(filter, update,
            new FindOneAndUpdateOptions<HubSpotOAuthState> { ReturnDocument = ReturnDocument.After }, ct);
    }

    public async Task<long> DeleteExpiredOrConsumedStatesAsync(DateTime olderThanUtc, CancellationToken ct)
    {
        var filter = Builders<HubSpotOAuthState>.Filter.Or(
            Builders<HubSpotOAuthState>.Filter.Lt(x => x.ExpiresAtUtc, olderThanUtc),
            Builders<HubSpotOAuthState>.Filter.Ne(x => x.UsedAtUtc, null));
        var res = await _states.DeleteManyAsync(filter, ct);
        return res.DeletedCount;
    }

    public async Task<HubSpotIntegration?> GetByTenantAsync(string tenantId, CancellationToken ct)
    {
       
        try
        {
           return await _integrations.Find(x => x.TenantId == tenantId).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving HubSpot integration for tenant {tenantId}: {ex.Message}");
            throw new HubSpotLookupException("HUBSPOT_INTEGRATION_ERROR", "Error retrieving HubSpot integration.");
        }
    }

    public async Task UpsertIntegrationAsync(HubSpotIntegration integration, CancellationToken ct = default)
    {
        integration.UpdatedAtUtc = DateTime.UtcNow;
        integration.Version += 1;
        var filter = Builders<HubSpotIntegration>.Filter.Eq(x => x.TenantId, integration.TenantId);
        await _integrations.ReplaceOneAsync(filter, integration,
            new ReplaceOptions { IsUpsert = true });
    }

    public async Task<bool> TryUpdateTokensAsync(HubSpotIntegration integration, long expectedVersion, CancellationToken ct)
    {
        integration.UpdatedAtUtc = DateTime.UtcNow;
        integration.Version = expectedVersion + 1;
        var filter = Builders<HubSpotIntegration>.Filter.And(
            Builders<HubSpotIntegration>.Filter.Eq(x => x.TenantId, integration.TenantId),
            Builders<HubSpotIntegration>.Filter.Eq(x => x.Version, expectedVersion));
        var res = await _integrations.ReplaceOneAsync(filter, integration, cancellationToken: ct);
        return res.ModifiedCount == 1;
    }
}
