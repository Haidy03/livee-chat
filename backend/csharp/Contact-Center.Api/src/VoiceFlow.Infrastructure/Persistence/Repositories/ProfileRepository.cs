using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class ProfileRepository : MongoRepository<Profile>, IProfileRepository
{
    public ProfileRepository(MongoDbContext context) : base(context, "profiles") { }

    public async Task<Profile?> GetByUserIdAndTenantAsync(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Profile>.Filter.And(
            Builders<Profile>.Filter.Eq(p => p.UserId, userId),
            Builders<Profile>.Filter.Eq(p => p.TenantId, tenantId));
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Profile?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Profile>.Filter.Eq(p => p.UserId, userId);
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Profile>> GetByTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Profile>.Filter.Eq(p => p.TenantId, tenantId);
        return await Collection.Find(filter).ToListAsync(cancellationToken);
    }
}
