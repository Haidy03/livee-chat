using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class FlowRepository : MongoRepository<Flow>, IFlowRepository
{
    public FlowRepository(MongoDbContext context) : base(context, "flows") { }

    public async Task<IEnumerable<Flow>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Flow>.Filter.Eq(f => f.TenantId, tenantId);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<Flow?> GetByExtensionAsync(string tenantId, string extension, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Flow>.Filter.And(
            Builders<Flow>.Filter.Eq(f => f.TenantId, tenantId),
            Builders<Flow>.Filter.Eq(f => f.AssignedExtension, extension));
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
