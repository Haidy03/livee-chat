using System.Collections.Concurrent;
using HelperLib.DataBase;
using HelperLib.Models.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Outbound.Event.Campaign.Lookups;

/// <summary>
/// Resolves a queue member's extension to the owning platform user id. Queue members are
/// registered as <c>PJSIP/{extensionNumber}</c> (see AsteriskExporter), so the agent that
/// handled an answered outbound call is known only by its extension on AMI events. Grouping
/// campaign reports by "agent" needs the platform userId, which lives on the profile.
/// Cached in-process (60s); a null return means no profile owns that extension.
/// </summary>
public interface IAgentLookupRepository
{
    Task<string?> GetUserIdByExtensionAsync(string tenantId, string extension, CancellationToken ct);
}

public sealed class AgentLookupRepository : IAgentLookupRepository
{
    private readonly IMongoCollection<BsonDocument> _profiles;
    private readonly ConcurrentDictionary<string, (DateTime exp, string? val)> _cache = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public AgentLookupRepository(MongoContext context, IOptions<MongoDbSettings> mongoSettings)
    {
        var db = context.GetDatabase(mongoSettings.Value.VoiceFlowDbName);
        _profiles = db.GetCollection<BsonDocument>("profiles");
    }

    public async Task<string?> GetUserIdByExtensionAsync(string tenantId, string extension, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(extension) || !int.TryParse(extension, out var ext))
            return null;

        var key = $"{tenantId}:{ext}";
        if (_cache.TryGetValue(key, out var hit) && hit.exp > DateTime.UtcNow)
            return hit.val;

        var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantId)
                     & Builders<BsonDocument>.Filter.Eq("extensionNumber", ext);
        var doc = await _profiles.Find(filter).FirstOrDefaultAsync(ct);

        var userId = doc?.GetValue("userId", BsonNull.Value);
        var value = userId is null || userId.IsBsonNull ? null : userId.AsString;

        _cache[key] = (DateTime.UtcNow.Add(Ttl), value);
        return value;
    }
}
