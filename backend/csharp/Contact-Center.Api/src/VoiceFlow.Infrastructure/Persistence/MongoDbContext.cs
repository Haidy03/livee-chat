using MongoDB.Driver;
using Microsoft.Extensions.Options;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Infrastructure.Persistence;

public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, IOptions<MongoDbSettings> options)
    {
        var settings = options.Value;
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string name) =>
        _database.GetCollection<T>(name);

    public IMongoDatabase Database => _database;
}
