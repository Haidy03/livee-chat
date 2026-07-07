using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Enums;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : MongoRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(MongoDbContext context) : base(context, "invoices") { }

    public async Task<(IEnumerable<Invoice> Items, long TotalCount)> SearchAsync(
        string tenantId, InvoiceStatus? status, DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Invoice>>
        {
            Builders<Invoice>.Filter.Eq(i => i.TenantId, tenantId)
        };

        if (status.HasValue) filters.Add(Builders<Invoice>.Filter.Eq(i => i.Status, status.Value));
        if (from.HasValue) filters.Add(Builders<Invoice>.Filter.Gte(i => i.IssueDate, from.Value));
        if (to.HasValue) filters.Add(Builders<Invoice>.Filter.Lte(i => i.IssueDate, to.Value));

        var combinedFilter = Builders<Invoice>.Filter.And(filters);
        var q = Collection.Find(combinedFilter).SortByDescending(i => i.IssueDate);
        var total = await q.CountDocumentsAsync(cancellationToken);
        var items = await q.Skip(skip).Limit(take).ToListAsync(cancellationToken);
        return (items, total);
    }
}
