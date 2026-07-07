using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class EmailThreadRepository : MongoRepository<EmailThread>, IEmailThreadRepository
{
    public EmailThreadRepository(MongoDbContext context) : base(context, "email_threads") { }

    public async Task<IReadOnlyList<EmailThread>> ListAsync(
        string tenantId, string? status, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<EmailThread>>
        {
            // Empty-tenant threads (unmapped mailbox) are visible to everyone.
            Builders<EmailThread>.Filter.In(t => t.TenantId, new[] { tenantId, string.Empty })
        };
        if (!string.IsNullOrWhiteSpace(status))
            filters.Add(Builders<EmailThread>.Filter.Eq(t => t.Status, status));

        return await Collection
            .Find(Builders<EmailThread>.Filter.And(filters))
            .SortByDescending(t => t.LastMessageAt)
            .Limit(200)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailThread?> FindBySubjectAndCounterpartAsync(
        string normalizedSubject, string counterpartEmail, CancellationToken cancellationToken = default)
    {
        var filter = Builders<EmailThread>.Filter.And(
            Builders<EmailThread>.Filter.Eq(t => t.NormalizedSubject, normalizedSubject),
            Builders<EmailThread>.Filter.Eq(t => t.CounterpartEmail, counterpartEmail));

        return await Collection
            .Find(filter)
            .SortByDescending(t => t.LastMessageAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task ApplyNewMessageAsync(
        string threadId, string snippet, DateTime at, string direction, bool hasAttachments,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<EmailThread>.Update
            .Set(t => t.LastMessageSnippet, snippet)
            .Set(t => t.LastMessageAt, at)
            .Set(t => t.LastMessageDirection, direction)
            .Set(t => t.LastMessageHasAttachments, hasAttachments)
            .Set(t => t.UpdatedAt, DateTime.UtcNow)
            .Inc(t => t.MessageCount, 1);

        if (direction == "inbound")
        {
            // New customer mail bumps unread, reopens a resolved/archived conversation
            // and wakes it from snooze.
            update = update
                .Inc(t => t.UnreadCount, 1)
                .Set(t => t.Status, "open")
                .Set(t => t.SnoozedUntil, null);
        }

        await Collection.UpdateOneAsync(
            Builders<EmailThread>.Filter.Eq(t => t.Id, threadId),
            update,
            cancellationToken: cancellationToken);
    }

    public async Task MarkReadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        await Collection.UpdateOneAsync(
            Builders<EmailThread>.Filter.Eq(t => t.Id, threadId),
            Builders<EmailThread>.Update
                .Set(t => t.UnreadCount, 0)
                .Set(t => t.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }

    public async Task MarkUnreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        await Collection.UpdateOneAsync(
            Builders<EmailThread>.Filter.And(
                Builders<EmailThread>.Filter.Eq(t => t.Id, threadId),
                Builders<EmailThread>.Filter.Eq(t => t.UnreadCount, 0)),
            Builders<EmailThread>.Update
                .Set(t => t.UnreadCount, 1)
                .Set(t => t.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }

    public async Task SetStatusAsync(string threadId, string status, CancellationToken cancellationToken = default)
    {
        var update = Builders<EmailThread>.Update
            .Set(t => t.Status, status)
            .Set(t => t.UpdatedAt, DateTime.UtcNow);

        if (status == "open")
        {
            update = update
                .Set(t => t.ResolvedBy, null)
                .Set(t => t.ResolvedAt, null)
                .Set(t => t.SnoozedUntil, null);
        }

        await Collection.UpdateOneAsync(
            Builders<EmailThread>.Filter.Eq(t => t.Id, threadId),
            update,
            cancellationToken: cancellationToken);
    }

    public async Task SetSnoozeAsync(string threadId, DateTime? until, CancellationToken cancellationToken = default)
    {
        await Collection.UpdateOneAsync(
            Builders<EmailThread>.Filter.Eq(t => t.Id, threadId),
            Builders<EmailThread>.Update
                .Set(t => t.SnoozedUntil, until)
                .Set(t => t.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }

    public async Task SetStarredAsync(string threadId, bool starred, CancellationToken cancellationToken = default)
    {
        await Collection.UpdateOneAsync(
            Builders<EmailThread>.Filter.Eq(t => t.Id, threadId),
            Builders<EmailThread>.Update
                .Set(t => t.Starred, starred)
                .Set(t => t.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }

    public async Task<bool> ResolveAsync(string threadId, string agentId, CancellationToken cancellationToken = default)
    {
        var result = await Collection.UpdateOneAsync(
            Builders<EmailThread>.Filter.And(
                Builders<EmailThread>.Filter.Eq(t => t.Id, threadId),
                Builders<EmailThread>.Filter.Ne(t => t.Status, "resolved")),
            Builders<EmailThread>.Update
                .Set(t => t.Status, "resolved")
                .Set(t => t.ResolvedBy, agentId)
                .Set(t => t.ResolvedAt, DateTime.UtcNow)
                .Set(t => t.UnreadCount, 0)
                .Set(t => t.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return result.ModifiedCount > 0;
    }
}
