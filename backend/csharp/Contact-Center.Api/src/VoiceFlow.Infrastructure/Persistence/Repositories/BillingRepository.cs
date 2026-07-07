using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class BillingRepository : MongoRepository<Billing>, IBillingRepository
{
    public BillingRepository(MongoDbContext context) : base(context, "billing") { }

    public async Task<Billing?> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Billing>.Filter.Eq(b => b.TenantId, tenantId);
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
