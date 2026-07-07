using MongoDB.Driver;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public abstract class MongoRepository<T> : IRepository<T> where T : Entity
{
    protected readonly IMongoCollection<T> Collection;

    protected MongoRepository(MongoDbContext context, string collectionName)
    {
        Collection = context.GetCollection<T>(collectionName);
    }

    public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(e => e.Id, id);
        return await Collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Collection.Find(Builders<T>.Filter.Empty).ToListAsync(cancellationToken);
    }

    public virtual async Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);
        await Collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
        return entity;
    }

    public virtual async Task<T> UpdateWithUpsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;

        var filter = Builders<T>.Filter.Eq(e => e.Id, entity.Id);

        var options = new ReplaceOptions
        {
            IsUpsert = true
        };

        await Collection.ReplaceOneAsync(
            filter,
            entity,
            options,
            cancellationToken);

        return entity;
    }

    public virtual async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<T>.Filter.Eq(e => e.Id, id);
        await Collection.DeleteOneAsync(filter, cancellationToken);
    }
}
