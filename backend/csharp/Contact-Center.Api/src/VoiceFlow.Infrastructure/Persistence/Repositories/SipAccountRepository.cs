using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class SipAccountRepository : MongoRepository<SipAccount>, ISipAccountRepository
{
    public SipAccountRepository(MongoDbContext context) : base(context, "sip_accounts") { }

    public async Task<SipAccount?> GetByUserAndTenantAsync(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<SipAccount>.Filter.And(
            Builders<SipAccount>.Filter.Eq(s => s.UserId, userId),
            Builders<SipAccount>.Filter.Eq(s => s.TenantId, tenantId));
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
