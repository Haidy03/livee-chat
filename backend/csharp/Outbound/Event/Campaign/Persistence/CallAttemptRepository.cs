using HelperLib.DataBase;
using HelperLib.Models.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Outbound.Event.Campaign.Models;

namespace Outbound.Event.Campaign.Persistence;

public interface ICallAttemptRepository
{
    Task StartAsync(CallAttempt seed, CancellationToken ct);
    Task RecordMilestoneAsync(string attemptId, UpdateDefinition<CallAttempt> update, CancellationToken ct);
    Task FinishAsync(string attemptId, string? disposition, UpdateDefinition<CallAttempt>? extra, CancellationToken ct);
}

public sealed class CallAttemptRepository : ICallAttemptRepository
{
    private readonly IMongoCollection<CallAttempt> _attempts;

    public CallAttemptRepository(MongoContext context, IOptions<MongoDbSettings> mongoSettings)
    {
        var db = context.GetDatabase(mongoSettings.Value.VoiceFlowDbName);
        _attempts = db.GetCollection<CallAttempt>("call_attempts");
    }

    public Task StartAsync(CallAttempt seed, CancellationToken ct) =>
        _attempts.InsertOneAsync(seed, cancellationToken: ct);

    public Task RecordMilestoneAsync(string attemptId, UpdateDefinition<CallAttempt> update, CancellationToken ct) =>
        _attempts.UpdateOneAsync(Builders<CallAttempt>.Filter.Eq(a => a.AttemptId, attemptId), update, cancellationToken: ct);

    public async Task FinishAsync(string attemptId, string? disposition, UpdateDefinition<CallAttempt>? extra, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var existing = await _attempts.Find(Builders<CallAttempt>.Filter.Eq(a => a.AttemptId, attemptId))
                                      .FirstOrDefaultAsync(ct);
        var duration = existing != null ? (now - existing.StartedAt).TotalSeconds : (double?)null;

        var update = Builders<CallAttempt>.Update
            .Set(a => a.EndedAt, now)
            .Set(a => a.DurationSec, duration)
            .Set(a => a.Disposition, disposition);

        if (extra != null)
            update = Builders<CallAttempt>.Update.Combine(update, extra);

        await _attempts.UpdateOneAsync(Builders<CallAttempt>.Filter.Eq(a => a.AttemptId, attemptId),
            update, cancellationToken: ct);
    }
}
