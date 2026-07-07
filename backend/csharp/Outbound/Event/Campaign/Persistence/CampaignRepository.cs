using HelperLib.DataBase;
using HelperLib.Models.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Outbound.Event.Campaign.Models;

namespace Outbound.Event.Campaign.Persistence;

/// <summary>
/// Mongo access for the campaign engine. All target state changes are expressed as conditional,
/// single-document atomic updates so they are idempotent and safe under concurrent dispatchers.
/// </summary>
public sealed class CampaignRepository
{
    private readonly IMongoCollection<CampaignModel> _campaigns;
    private readonly IMongoCollection<CampaignTarget> _targets;
    private readonly IMongoCollection<CampaignTargetResult> _results;

    public CampaignRepository(MongoContext context, IOptions<MongoDbSettings> mongoSettings)
    {
        var db = context.GetDatabase(mongoSettings.Value.VoiceFlowDbName);
        _campaigns = db.GetCollection<CampaignModel>("campaigns");
        _targets = db.GetCollection<CampaignTarget>("campaign_targets");
        _results = db.GetCollection<CampaignTargetResult>("campaigntargetresulttest");
    }

    public Task InsertResultAsync(CampaignTargetResult result, CancellationToken ct) =>
        _results.InsertOneAsync(result, cancellationToken: ct);

    public async Task<IReadOnlyList<CampaignModel>> GetActiveCampaignsAsync(CancellationToken ct)
    {
        var filter = Builders<CampaignModel>.Filter.Eq(c => c.Status, "active");
        return await _campaigns.Find(filter).ToListAsync(ct);
    }

    /// <summary>
    /// Pull-based claim used by the new dispatcher: atomically flip the next dialable target from
    /// <c>pending → dialing</c>, stamping <c>dialingAt</c> and a fresh <c>attemptId</c>. A target is
    /// dialable when its status is pending, its backoff gate has elapsed (or is unset), and its
    /// attempts are below the configured max. Retries return to pending with <c>nextAttemptAt</c>,
    /// so this same filter naturally re-claims them when due.
    /// </summary>
    public async Task<CampaignTarget?> ClaimNextDialableTargetAsync(string campaignId, int maxAttempts, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.CampaignId, campaignId),
            Builders<CampaignTarget>.Filter.Eq(t => t.Status, "pending"),
            Builders<CampaignTarget>.Filter.Or(
                Builders<CampaignTarget>.Filter.Eq(t => t.NextAttemptAt, null),
                Builders<CampaignTarget>.Filter.Lte(t => t.NextAttemptAt, now)),
            // A missing 'attempts' field (e.g. manually-inserted targets) must count as 0 —
            // MongoDB's $lt does not match absent fields, so allow "not exists" too.
            Builders<CampaignTarget>.Filter.Or(
                Builders<CampaignTarget>.Filter.Exists(t => t.Attempts, false),
                Builders<CampaignTarget>.Filter.Lt(t => t.Attempts, maxAttempts)));

        var attemptId = Guid.NewGuid().ToString("N");
        var update = Builders<CampaignTarget>.Update
            .Set(t => t.Status, "dialing")
            .Set(t => t.DialingAt, now)
            .Set(t => t.AttemptId, attemptId);

        return await _targets.FindOneAndUpdateAsync(
            filter, update,
            new FindOneAndUpdateOptions<CampaignTarget> { ReturnDocument = ReturnDocument.After },
            ct);
    }

    /// <summary>Revert a dialing target to pending (used when the originate write fails).</summary>
    public async Task RevertDialingToPendingAsync(string targetId, CancellationToken ct)
    {
        var filter = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.Id, targetId),
            Builders<CampaignTarget>.Filter.Eq(t => t.Status, "dialing"));
        var update = Builders<CampaignTarget>.Update
            .Set(t => t.Status, "pending")
            .Unset(t => t.AttemptId)
            .Unset(t => t.DialingAt);
        await _targets.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task<CampaignTarget?> GetTargetAsync(string targetId, CancellationToken ct)
    {
        var filter = Builders<CampaignTarget>.Filter.Eq(t => t.Id, targetId);
        return await _targets.Find(filter).FirstOrDefaultAsync(ct);
    }

    /// <summary>Stale <c>dialing</c> rows the reaper should examine.</summary>
    public async Task<IReadOnlyList<CampaignTarget>> GetStaleDialingTargetsAsync(TimeSpan olderThan, int limit, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow - olderThan;
        var filter = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.Status, "dialing"),
            Builders<CampaignTarget>.Filter.Lt(t => t.DialingAt, cutoff));
        return await _targets.Find(filter).Limit(limit).ToListAsync(ct);
    }

    /// <summary>Count of currently dialing targets — used by the reaper's counter reconciliation.</summary>
    public async Task<long> CountDialingAsync(string campaignId, CancellationToken ct)
    {
        var filter = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.CampaignId, campaignId),
            Builders<CampaignTarget>.Filter.Eq(t => t.Status, "dialing"));
        return await _targets.CountDocumentsAsync(filter, cancellationToken: ct);
    }

    /// <summary>Conditional status flip. Returns true only if the target was in <paramref name="from"/>.</summary>
    public async Task<bool> TryTransitionStatusAsync(string targetId, string from, string to, CancellationToken ct)
    {
        var filter = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.Id, targetId),
            Builders<CampaignTarget>.Filter.Eq(t => t.Status, from));

        var update = Builders<CampaignTarget>.Update.Set(t => t.Status, to);
        var res = await _targets.UpdateOneAsync(filter, update, cancellationToken: ct);
        return res.ModifiedCount == 1;
    }

    /// <summary>Move a target from <paramref name="from"/> to a terminal state and record disposition.</summary>
    public async Task<bool> CompleteTargetAsync(string targetId, string from, string terminalStatus,
        string? disposition, CancellationToken ct)
    {
        var filter = Builders<CampaignTarget>.Filter.And(
            Builders<CampaignTarget>.Filter.Eq(t => t.Id, targetId),
            Builders<CampaignTarget>.Filter.Eq(t => t.Status, from));

        var update = Builders<CampaignTarget>.Update
            .Set(t => t.Status, terminalStatus)
            .Set(t => t.LastDisposition, disposition)
            .Unset(t => t.AttemptId)
            .Unset(t => t.DialingAt)
            .Set("lastCallAt", DateTime.UtcNow.ToString("O"));

        var res = await _targets.UpdateOneAsync(filter, update, cancellationToken: ct);
        return res.ModifiedCount == 1;
    }

    /// <summary>Record a transient retry: bump attempts, set the backoff gate, return target to pending.</summary>
    public async Task ScheduleRetryAsync(string targetId, DateTime nextAttemptAt, string? disposition,
        string? reason, bool countsAsAttempt, CancellationToken ct)
    {
        var filter = Builders<CampaignTarget>.Filter.Eq(t => t.Id, targetId);

        var update = Builders<CampaignTarget>.Update
            .Set(t => t.Status, "pending")
            .Set(t => t.NextAttemptAt, nextAttemptAt)
            .Set(t => t.LastDisposition, disposition)
            .Set(t => t.LastError, reason)
            .Unset(t => t.AttemptId)
            .Unset(t => t.DialingAt);

        if (countsAsAttempt)
            update = update.Inc(t => t.Attempts, 1);

        await _targets.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task FailTargetAsync(string targetId, string? disposition, string? reason, CancellationToken ct)
    {
        var filter = Builders<CampaignTarget>.Filter.Eq(t => t.Id, targetId);
        var update = Builders<CampaignTarget>.Update
            .Set(t => t.Status, "failed")
            .Set(t => t.LastDisposition, disposition)
            .Set(t => t.LastError, reason)
            .Unset(t => t.AttemptId)
            .Unset(t => t.DialingAt);

        await _targets.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task IncrementCountersAsync(string campaignId, string terminalStatus, CancellationToken ct)
    {
        var filter = Builders<CampaignModel>.Filter.Eq(c => c.Id, campaignId);

        var update = Builders<CampaignModel>.Update
            .Inc("targetsCalled", 1)
            .Set("lastActivityAt", DateTime.UtcNow);

        update = terminalStatus switch
        {
            "successful" => update.Inc("targetsSuccessful", 1),
            "callback" => update.Inc("targetsCallback", 1),
            "failed" => update.Inc("targetsFailed", 1),
            _ => update
        };

        await _campaigns.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    /// <summary>Flip active → completed once no non-terminal targets remain.</summary>
    public async Task<bool> TryCompleteCampaignAsync(string campaignId, CancellationToken ct)
    {
        var nonTerminal = new[] { "successful", "callback", "failed", "cancelled" };
        var remaining = await _targets.CountDocumentsAsync(
            Builders<CampaignTarget>.Filter.And(
                Builders<CampaignTarget>.Filter.Eq(t => t.CampaignId, campaignId),
                Builders<CampaignTarget>.Filter.Nin(t => t.Status, nonTerminal)),
            cancellationToken: ct);

        if (remaining > 0) return false;

        var res = await _campaigns.UpdateOneAsync(
            Builders<CampaignModel>.Filter.And(
                Builders<CampaignModel>.Filter.Eq(c => c.Id, campaignId),
                Builders<CampaignModel>.Filter.Eq(c => c.Status, "active")),
            Builders<CampaignModel>.Update
                .Set(c => c.Status, "completed")
                .Set("completedAt", DateTime.UtcNow),
            cancellationToken: ct);

        return res.ModifiedCount == 1;
    }
}
