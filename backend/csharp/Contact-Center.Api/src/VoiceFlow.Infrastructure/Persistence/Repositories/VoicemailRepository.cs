using MongoDB.Driver;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Infrastructure.Persistence.Repositories;

public sealed class VoicemailRepository : MongoRepository<Voicemail>, IVoicemailRepository
{
    // Shared with the FreeSwitchXmlCurl webhook writer — all three services must point at
    // the same database + this collection (see FreeSwitch:VoicemailCollectionName).
    public VoicemailRepository(MongoDbContext context) : base(context, "voicemail_messages") { }

    public async Task<bool> SetProcessingResultAsync(
        string id,
        string s3Url,
        CallAnalysisResult? analysis,
        CancellationToken cancellationToken = default)
    {
        var update = Builders<Voicemail>.Update
            .Set(v => v.S3Url, s3Url)
            .Set(v => v.UpdatedAt, DateTime.UtcNow);

        if (analysis is not null)
        {
            if (!string.IsNullOrWhiteSpace(analysis.Transcript))
                update = update.Set(v => v.Transcript, analysis.Transcript);
            if (!string.IsNullOrWhiteSpace(analysis.Summary))
                update = update.Set(v => v.Summary, analysis.Summary);
            if (!string.IsNullOrWhiteSpace(analysis.Sentiment?.Overall))
                update = update.Set(v => v.Sentiment, analysis.Sentiment!.Overall);
        }

        var result = await Collection.UpdateOneAsync(
            Builders<Voicemail>.Filter.Eq(v => v.Id, id),
            update,
            cancellationToken: cancellationToken);

        return result.MatchedCount > 0;
    }

    public async Task<IReadOnlyList<Voicemail>> ListForOwnersAsync(
        string tenantId,
        IEnumerable<string> ownerIds,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Voicemail>>
        {
            Builders<Voicemail>.Filter.Eq(v => v.TenantId, tenantId),
            Builders<Voicemail>.Filter.In(v => v.OwnerId, ownerIds)
        };
        if (!string.IsNullOrWhiteSpace(status))
            filters.Add(Builders<Voicemail>.Filter.Eq(v => v.Status, status));

        return await Collection
            .Find(Builders<Voicemail>.Filter.And(filters))
            .SortByDescending(v => v.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, int>> CountNewByOwnerAsync(
        string tenantId,
        IEnumerable<string> ownerIds,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Voicemail>.Filter.And(
            Builders<Voicemail>.Filter.Eq(v => v.TenantId, tenantId),
            Builders<Voicemail>.Filter.In(v => v.OwnerId, ownerIds),
            Builders<Voicemail>.Filter.Eq(v => v.Status, "new"));

        var docs = await Collection
            .Find(filter)
            .Project(v => v.OwnerId)
            .ToListAsync(cancellationToken);

        return docs
            .GroupBy(o => o)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<bool> TryClaimAsync(
        string tenantId,
        string id,
        string agentId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Voicemail>.Filter.And(
            Builders<Voicemail>.Filter.Eq(v => v.Id, id),
            Builders<Voicemail>.Filter.Eq(v => v.TenantId, tenantId),
            Builders<Voicemail>.Filter.Eq(v => v.Status, "new"));

        var update = Builders<Voicemail>.Update
            .Set(v => v.Status, "claimed")
            .Set(v => v.ClaimedBy, agentId)
            .Set(v => v.ClaimedAt, DateTime.UtcNow)
            .Set(v => v.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public async Task<bool> ResolveAsync(
        string tenantId,
        string id,
        string agentId,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<Voicemail>.Filter.And(
            Builders<Voicemail>.Filter.Eq(v => v.Id, id),
            Builders<Voicemail>.Filter.Eq(v => v.TenantId, tenantId),
            Builders<Voicemail>.Filter.Ne(v => v.Status, "done"));

        var update = Builders<Voicemail>.Update
            .Set(v => v.Status, "done")
            .Set(v => v.ResolvedBy, agentId)
            .Set(v => v.ResolvedAt, DateTime.UtcNow)
            .Set(v => v.UpdatedAt, DateTime.UtcNow);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }
}
