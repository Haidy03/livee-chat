using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class AuthUserRepository : MongoRepository<AuthUser>, IAuthUserRepository
{
    public AuthUserRepository(MongoDbContext context) : base(context, "auth_users")
    {
        Collection.Indexes.CreateOne(
            new CreateIndexModel<AuthUser>(
                Builders<AuthUser>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true }));
    }

    public async Task<AuthUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuthUser>.Filter.Eq(u => u.Email, email.ToLowerInvariant());
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AuthUser?> GetByResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var filter = Builders<AuthUser>.Filter.Eq(u => u.PasswordResetToken, token);
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
