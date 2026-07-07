using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using VoiceFlow.Application.Common;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Infrastructure.Persistence;

namespace VoiceFlow.Api.Controllers;

/// <summary>
/// One-off maintenance: migrate embedded contacts/activity/receivedCalls arrays from the
/// legacy <c>campaigns</c> documents into their dedicated collections. Idempotent — campaigns
/// whose embedded arrays are already empty/absent are skipped.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/maintenance/campaigns")]
public sealed class CampaignMigrationController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly ICampaignRepository _campaigns;
    private readonly ICampaignTargetRepository _targets;
    private readonly ICampaignActivityRepository _activity;
    private readonly ICampaignReceivedCallRepository _receivedCalls;
    private readonly ICurrentUser _currentUser;

    public CampaignMigrationController(
        MongoDbContext db,
        ICampaignRepository campaigns,
        ICampaignTargetRepository targets,
        ICampaignActivityRepository activity,
        ICampaignReceivedCallRepository receivedCalls,
        ICurrentUser currentUser)
    {
        _db = db;
        _campaigns = campaigns;
        _targets = targets;
        _activity = activity;
        _receivedCalls = receivedCalls;
        _currentUser = currentUser;
    }

    public sealed class MigrationReport
    {
        public int CampaignsScanned { get; set; }
        public int CampaignsMigrated { get; set; }
        public long TargetsMoved { get; set; }
        public long ActivityMoved { get; set; }
        public long ReceivedCallsMoved { get; set; }
    }

    [HttpPost("migrate-embedded-arrays")]
    public async Task<IActionResult> Migrate(CancellationToken ct)
    {
        // Read raw BSON so we can see legacy embedded fields that no longer exist on Campaign.
        var raw = _db.GetCollection<BsonDocument>("campaigns");
        var report = new MigrationReport();

        using var cursor = await raw.Find(FilterDefinition<BsonDocument>.Empty).ToCursorAsync(ct);
        while (await cursor.MoveNextAsync(ct))
        {
            foreach (var doc in cursor.Current)
            {
                report.CampaignsScanned++;
                var campaignId = doc["_id"].AsObjectId.ToString();
                var tenantId = doc.GetValue("tenantId", BsonString.Empty).AsString;
                if (string.IsNullOrEmpty(tenantId)) continue;

                var targets = ExtractTargets(doc, tenantId, campaignId);
                var activity = ExtractActivity(doc, tenantId, campaignId);
                var received = ExtractReceivedCalls(doc, tenantId, campaignId);

                var touched = false;

                if (targets.Count > 0)
                {
                    await _targets.InsertManyAsync(targets, ct);
                    report.TargetsMoved += targets.Count;
                    touched = true;
                }
                if (activity.Count > 0)
                {
                    await _activity.InsertManyAsync(activity, ct);
                    report.ActivityMoved += activity.Count;
                    touched = true;
                }
                if (received.Count > 0)
                {
                    await _receivedCalls.InsertManyAsync(received, ct);
                    report.ReceivedCallsMoved += received.Count;
                    touched = true;
                }

                // Compute counters and clear embedded arrays, also drop version if missing.
                var update = Builders<BsonDocument>.Update
                    .Unset("contacts")
                    .Unset("activity")
                    .Unset("receivedCalls")
                    .Set("targetsTotal", targets.Count)
                    .Set("targetsPending", targets.Count(t => t.Status == "pending"))
                    .Set("targetsCalled", targets.Count(t => t.Status == "called"))
                    .Set("targetsSuccessful", targets.Count(t => t.Status == "successful"))
                    .Set("targetsFailed", targets.Count(t => t.Status == "failed"))
                    .Set("targetsCallback", targets.Count(t => t.Status == "callback"));

                await raw.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]),
                    update,
                    cancellationToken: ct);

                if (touched) report.CampaignsMigrated++;
            }
        }

        return Ok(ApiResponse<MigrationReport>.Ok(report));
    }

    private static List<CampaignTarget> ExtractTargets(BsonDocument doc, string tenantId, string campaignId)
    {
        var list = new List<CampaignTarget>();
        if (!doc.TryGetValue("contacts", out var v) || v.BsonType != BsonType.Array) return list;
        var now = DateTime.UtcNow;
        foreach (var item in v.AsBsonArray)
        {
            if (item is not BsonDocument c) continue;
            list.Add(new CampaignTarget
            {
                Id = c.GetValue("id", Guid.NewGuid().ToString("N")).ToString()!,
                TenantId = tenantId,
                CampaignId = campaignId,
                FirstName = c.GetValue("firstName", BsonString.Empty).AsString,
                LastName = c.GetValue("lastName", BsonString.Empty).AsString,
                Phone = c.GetValue("phone", BsonString.Empty).AsString,
                Email = c.TryGetValue("email", out var em) && em.BsonType == BsonType.String ? em.AsString : null,
                Notes = c.TryGetValue("notes", out var nt) && nt.BsonType == BsonType.String ? nt.AsString : null,
                Status = c.GetValue("status", new BsonString("pending")).AsString,
                LastCallAt = c.TryGetValue("lastCallAt", out var lc) && lc.BsonType == BsonType.String ? lc.AsString : null,
                Source = c.GetValue("source", new BsonString("manual")).AsString,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        return list;
    }

    private static List<CampaignActivityItem> ExtractActivity(BsonDocument doc, string tenantId, string campaignId)
    {
        var list = new List<CampaignActivityItem>();
        if (!doc.TryGetValue("activity", out var v) || v.BsonType != BsonType.Array) return list;
        var now = DateTime.UtcNow;
        foreach (var item in v.AsBsonArray)
        {
            if (item is not BsonDocument a) continue;
            list.Add(new CampaignActivityItem
            {
                Id = a.GetValue("id", Guid.NewGuid().ToString("N")).ToString()!,
                TenantId = tenantId,
                CampaignId = campaignId,
                At = a.GetValue("at", BsonString.Empty).AsString,
                Type = a.GetValue("type", new BsonString("created")).AsString,
                Message = a.GetValue("message", BsonString.Empty).AsString,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        return list;
    }

    private static List<CampaignReceivedCallItem> ExtractReceivedCalls(BsonDocument doc, string tenantId, string campaignId)
    {
        var list = new List<CampaignReceivedCallItem>();
        if (!doc.TryGetValue("receivedCalls", out var v) || v.BsonType != BsonType.Array) return list;
        var now = DateTime.UtcNow;
        foreach (var item in v.AsBsonArray)
        {
            if (item is not BsonDocument r) continue;
            list.Add(new CampaignReceivedCallItem
            {
                Id = r.GetValue("id", Guid.NewGuid().ToString("N")).ToString()!,
                TenantId = tenantId,
                CampaignId = campaignId,
                CallerName = r.GetValue("callerName", BsonString.Empty).AsString,
                Phone = r.GetValue("phone", BsonString.Empty).AsString,
                At = r.GetValue("at", BsonString.Empty).AsString,
                DurationSec = r.GetValue("durationSec", new BsonInt32(0)).ToInt32(),
                WaitSec = r.TryGetValue("waitSec", out var ws) && ws.IsNumeric ? ws.ToInt32() : (int?)null,
                AgentId = r.TryGetValue("agentId", out var ag) && ag.BsonType == BsonType.String ? ag.AsString : null,
                Status = r.GetValue("status", new BsonString("resolved")).AsString,
                Notes = r.TryGetValue("notes", out var nt) && nt.BsonType == BsonType.String ? nt.AsString : null,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        return list;
    }
}
