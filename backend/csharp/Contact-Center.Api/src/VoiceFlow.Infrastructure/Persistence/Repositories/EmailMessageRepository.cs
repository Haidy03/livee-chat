using MongoDB.Driver;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class EmailMessageRepository : MongoRepository<EmailMessage>, IEmailMessageRepository
{
    public EmailMessageRepository(MongoDbContext context) : base(context, "email_messages") { }

    public async Task<IReadOnlyList<EmailMessage>> ListByThreadAsync(
        string threadId, CancellationToken cancellationToken = default)
    {
        return await Collection
            .Find(Builders<EmailMessage>.Filter.Eq(m => m.ThreadId, threadId))
            .SortBy(m => m.SentAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByMessageIdAsync(string messageId, CancellationToken cancellationToken = default)
    {
        var count = await Collection.CountDocumentsAsync(
            Builders<EmailMessage>.Filter.Eq(m => m.MessageId, messageId),
            new CountOptions { Limit = 1 },
            cancellationToken);
        return count > 0;
    }

    public async Task<string?> FindThreadIdByMessageIdsAsync(
        IReadOnlyCollection<string> messageIds, CancellationToken cancellationToken = default)
    {
        if (messageIds.Count == 0) return null;

        var match = await Collection
            .Find(Builders<EmailMessage>.Filter.In(m => m.MessageId, messageIds))
            .SortByDescending(m => m.SentAt)
            .FirstOrDefaultAsync(cancellationToken);

        return match?.ThreadId;
    }

    public async Task<EmailMessage?> GetLatestInboundAsync(string threadId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<EmailMessage>.Filter.And(
            Builders<EmailMessage>.Filter.Eq(m => m.ThreadId, threadId),
            Builders<EmailMessage>.Filter.Eq(m => m.Direction, "inbound"));

        return await Collection
            .Find(filter)
            .SortByDescending(m => m.SentAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
