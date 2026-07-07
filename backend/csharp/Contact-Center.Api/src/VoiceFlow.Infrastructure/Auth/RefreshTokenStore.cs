using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Core.Interfaces.Services;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Auth;

public sealed class RefreshTokenDocument
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RefreshTokenStore : IRefreshTokenStore
{
    private readonly IMongoCollection<RefreshTokenDocument> _collection;

    public RefreshTokenStore(MongoDbContext context)
    {
        _collection = context.GetCollection<RefreshTokenDocument>("refresh_tokens");
        EnsureIndexes();
    }

    private void EnsureIndexes()
    {
        var indexModels = new List<CreateIndexModel<RefreshTokenDocument>>
        {
            new(Builders<RefreshTokenDocument>.IndexKeys.Ascending(t => t.Token)),
            new(Builders<RefreshTokenDocument>.IndexKeys
                .Ascending(t => t.UserId)
                .Ascending(t => t.TenantId)),
            new(Builders<RefreshTokenDocument>.IndexKeys.Ascending(t => t.ExpiresAt),
                new CreateIndexOptions { ExpireAfter = TimeSpan.Zero })
        };
        _collection.Indexes.CreateMany(indexModels);
    }

    public async Task SaveAsync(string userId, string tenantId, string token, DateTime expiresAt, CancellationToken ct = default)
    {
        var doc = new RefreshTokenDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = userId,
            TenantId = tenantId,
            Token = token,
            ExpiresAt = expiresAt
        };
        await _collection.InsertOneAsync(doc, cancellationToken: ct);
    }

    public async Task<(string UserId, string TenantId, bool IsValid)> ValidateAsync(string token, CancellationToken ct = default)
    {
        var filter = Builders<RefreshTokenDocument>.Filter.Eq(t => t.Token, token);
        var doc = await _collection.Find(filter).FirstOrDefaultAsync(ct);

        if (doc is null || doc.IsRevoked || doc.ExpiresAt < DateTime.UtcNow)
            return (string.Empty, string.Empty, false);

        return (doc.UserId, doc.TenantId, true);
    }

    public async Task RevokeAsync(string token, CancellationToken ct = default)
    {
        var filter = Builders<RefreshTokenDocument>.Filter.Eq(t => t.Token, token);
        var update = Builders<RefreshTokenDocument>.Update.Set(t => t.IsRevoked, true);
        await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task RevokeAllForUserAsync(string userId, string tenantId, CancellationToken ct = default)
    {
        var filter = Builders<RefreshTokenDocument>.Filter.And(
            Builders<RefreshTokenDocument>.Filter.Eq(t => t.UserId, userId),
            Builders<RefreshTokenDocument>.Filter.Eq(t => t.TenantId, tenantId));
        var update = Builders<RefreshTokenDocument>.Update.Set(t => t.IsRevoked, true);
        await _collection.UpdateManyAsync(filter, update, cancellationToken: ct);
    }
}
