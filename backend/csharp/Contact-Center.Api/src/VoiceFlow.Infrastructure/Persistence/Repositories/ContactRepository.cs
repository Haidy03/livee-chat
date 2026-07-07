using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class ContactRepository : MongoRepository<Contact>, IContactRepository
{
    public ContactRepository(MongoDbContext context) : base(context, "contacts") { }

    public async Task<(IEnumerable<Contact> Items, long TotalCount)> SearchAsync(
        string tenantId, string? query, IEnumerable<string>? tagIds, int skip, int take, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Contact>>
        {
            Builders<Contact>.Filter.Eq(c => c.TenantId, tenantId)
        };

        if (!string.IsNullOrEmpty(query))
        {
            var regex = new MongoDB.Bson.BsonRegularExpression(query, "i");
            filters.Add(Builders<Contact>.Filter.Or(
                Builders<Contact>.Filter.Regex(c => c.Name, regex),
                Builders<Contact>.Filter.Regex(c => c.Phone, regex),
                Builders<Contact>.Filter.Regex(c => c.Email, regex)));
        }

        if (tagIds?.Any() == true)
            filters.Add(Builders<Contact>.Filter.AnyIn(c => c.TagIds, tagIds));

        var combinedFilter = Builders<Contact>.Filter.And(filters);
        var query2 = Collection.Find(combinedFilter);
        var totalCount = await query2.CountDocumentsAsync(cancellationToken);
        var items = await query2.Skip(skip).Limit(take).ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
