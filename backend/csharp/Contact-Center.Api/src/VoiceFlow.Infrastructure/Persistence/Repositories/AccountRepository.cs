using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository : MongoRepository<Account>, IAccountRepository
{
    public AccountRepository(MongoDbContext context) : base(context, "accounts") { }

    public async Task<Account?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Account>.Filter.Eq(a => a.UserId, userId);
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
