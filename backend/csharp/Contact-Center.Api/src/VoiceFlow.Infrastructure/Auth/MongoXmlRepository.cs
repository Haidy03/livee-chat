using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace VoiceFlow.Infrastructure.Auth;

/// <summary>
/// Persists the Data Protection key ring in MongoDB so encrypted
/// HubSpot tokens remain decryptable across restarts and replicas.
/// </summary>
public sealed class MongoXmlRepository : IXmlRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoXmlRepository(IMongoCollection<BsonDocument> collection)
    {
        _collection = collection;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        var docs = _collection.Find(FilterDefinition<BsonDocument>.Empty).ToList();
        var list = new List<XElement>(docs.Count);
        foreach (var d in docs)
        {
            if (d.TryGetValue("Xml", out var v) && v.IsString)
                list.Add(XElement.Parse(v.AsString));
        }
        return list;
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        _collection.InsertOne(new BsonDocument
        {
            { "_id", ObjectId.GenerateNewId() },
            { "FriendlyName", friendlyName ?? string.Empty },
            { "Xml", element.ToString(SaveOptions.DisableFormatting) },
            { "CreatedAtUtc", DateTime.UtcNow },
        });
    }
}
