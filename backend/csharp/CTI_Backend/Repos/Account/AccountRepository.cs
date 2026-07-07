using CTI.Models.Directory;
using CtiBackend.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using PhoneNumbers;


namespace VoiceFlow.Repos;

public sealed class AccountRepository : IAccountRepository
{
    private readonly IMongoCollection<Account> _collection;
    public AccountRepository(IMongoClient client, IOptions<MongoOptions> options) {
        var opts = options.Value;
        _collection = client.GetDatabase(opts.Database).GetCollection<Account>(opts.AccountsCollection);
    }

    public async Task<Account?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Account>.Filter.Eq(a => a.UserId, userId);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Account?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Account>.Filter.Eq(e => e.Id, id);
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
}
