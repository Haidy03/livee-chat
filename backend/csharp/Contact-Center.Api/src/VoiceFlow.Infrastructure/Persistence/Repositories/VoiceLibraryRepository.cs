using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class VoiceLibraryRepository : MongoRepository<VoiceLibraryItem>, IVoiceLibraryRepository
{
    public VoiceLibraryRepository(MongoDbContext context) : base(context, "voice_library_items") { }

    public async Task<IEnumerable<VoiceLibraryItem>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<VoiceLibraryItem>.Filter.Eq(v => v.TenantId, tenantId);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
}
