using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Drawing;
using VoiceFlow.Application.Interfaces.Reports;
using VoiceFlow.Core.Entities;
using VoiceFlow.Infrastructure.Persistence;
namespace VoiceFlow.Infrastructure.Auth;

/// <summary>
/// Mongo-backed RBAC authorizer. Reads RbacRole + RbacUserRole collections (constitution XVII).
/// Permissions document shape: { reports: ["view","create","edit","delete","export"], ... }
/// </summary>
public sealed class RbacAuthorizer : IRbacAuthorizer
{
    private const string Module = "reports";
    private readonly IMongoCollection<RbacUserRole> _userRoles;
    private readonly IMongoCollection<RbacRole> _roles;
    private readonly IMemoryCache _cache;

    public RbacAuthorizer(MongoDbContext context, IMemoryCache cache)
    {
        _userRoles = context.GetCollection<RbacUserRole>("rbac_user_roles");
        _roles = context.GetCollection<RbacRole>("rbac_roles");
        _cache = cache;
    }

    public async Task<bool> CanAsync(string tenantId, string userId, ReportsAction action, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(userId)) return false;
        var key = $"rbac:{tenantId}:{userId}:reports";
        var allowed = await _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            return await LoadActionsAsync(tenantId, userId, ct);
        }) ?? new HashSet<string>();
        return allowed.Contains(action.ToString().ToLowerInvariant());
    }

    private async Task<HashSet<string>> LoadActionsAsync(string tenantId, string userId, CancellationToken ct)
    {
        var roleAssignments = await _userRoles
            .Find(x=>x.TenantId == tenantId && x.UserId == userId)
            .ToListAsync(ct);

        var roleIds = roleAssignments
            .Select(d => d.RoleId)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (roleIds.Count == 0) return new HashSet<string>();

        var roleObjectIds = roleIds.Select(ObjectId.Parse).ToList();
        var roles = await _roles
            .Find(Builders<RbacRole>.Filter.In("_id", roleObjectIds))
            .ToListAsync(ct);

        var actions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in roles)
        {
            if (r.Permissions.TryGetValue(Module, out var permissions))
            {
                actions.UnionWith(permissions);
            }
        }
        return actions;
    }
}
