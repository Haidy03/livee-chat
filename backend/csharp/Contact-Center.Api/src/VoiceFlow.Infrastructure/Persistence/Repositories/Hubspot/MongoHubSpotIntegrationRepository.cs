using Microsoft.Extensions.Options;
using MongoDB.Driver;
using VoiceFlow.Application.Options;
using VoiceFlow.Core.Entities.HubSpot;
using VoiceFlow.Core.Interfaces.Repositories.Hubspot;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Infrastructure.Persistence.Repositories.Hubspot
{
    public sealed class MongoHubSpotIntegrationRepository : IHubSpotIntegrationRepository
    {
        private readonly IMongoCollection<HubSpotOAuthState> _states;
        private readonly IMongoCollection<HubSpotIntegration> _integrations;

        public MongoHubSpotIntegrationRepository(IMongoClient client,
                                                 IOptions<MongoDbSettings> mongo,
                                                 IOptions<HubSpotOptions> hubspot)
        {
            var db = client.GetDatabase(mongo.Value.DatabaseName);
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
            return await _states.Find(filter).FirstOrDefaultAsync();
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
            => await _integrations.Find(x => x.TenantId == tenantId).FirstOrDefaultAsync(ct);

        public async Task UpsertIntegrationAsync(HubSpotIntegration integration, CancellationToken ct)
        {
            integration.UpdatedAtUtc = DateTime.UtcNow;
            integration.Version += 1;
            var filter = Builders<HubSpotIntegration>.Filter.Eq(x => x.TenantId, integration.TenantId);

            try
            {
                await _integrations.ReplaceOneAsync(filter, integration,
                    new ReplaceOptions { IsUpsert = true }, ct);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            
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
}
