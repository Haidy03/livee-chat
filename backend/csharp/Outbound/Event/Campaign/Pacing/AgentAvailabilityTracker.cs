using System.Collections.Concurrent;
using Outbound.Event.Campaign.Lookups;
using Outbound.Event.Campaign.Models;
using Outbound.Infrastructure.Ami;

namespace Outbound.Event.Campaign.Pacing;

/// <summary>
/// Per-queue free-agent counter. The Redis-backed implementation (see
/// <see cref="RedisAgentAvailabilityTracker"/>) is the default; this AMI-fed implementation is
/// the fallback selected via <c>AgentAvailability:Source = "ami"</c>.
/// </summary>
public interface IAgentAvailabilityTracker
{
    /// <summary>Legacy per-campaign lookup — kept for the AMI tracker's internal use.</summary>
    int GetFreeAgents(string campaignId);

    /// <summary>Preferred lookup used by the pull dispatcher. Takes a fully-resolved queue name
    /// so both <c>assignedMode</c> variants work.</summary>
    Task<int> GetFreeAgentsForCampaignAsync(CampaignModel campaign, CancellationToken ct);

    Task<int> GetFreeAgentsForQueueAsync(string tenantId, string queueName, CancellationToken ct);
}

/// <summary>
/// AMI-fed free-agent counter (fallback). Assumes the queue name convention
/// <c>t_{tenantId}__qc_{campaignId}</c>, so it can only serve campaigns dialing their own derived
/// queue. Shared-queue campaigns must use the Redis tracker.
/// </summary>
public sealed class AgentAvailabilityTracker : IAgentAvailabilityTracker, IAmiEventHandler
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, AgentState>> _queues = new();
    private readonly ILogger<AgentAvailabilityTracker> _log;

    public AgentAvailabilityTracker(ILogger<AgentAvailabilityTracker> log) { _log = log; }

    private sealed class AgentState
    {
        public int Status;   // 1 = NotInUse (free)
        public bool Paused;
        public bool OnCall;
    }

    public int GetFreeAgents(string campaignId) =>
        _queues.TryGetValue(campaignId, out var members)
            ? members.Values.Count(a => !a.Paused && !a.OnCall && a.Status == 1)
            : 0;

    public Task<int> GetFreeAgentsForCampaignAsync(CampaignModel campaign, CancellationToken ct)
    {
        // AMI tracker only knows the derived campaign queue. For assignedMode=="queue" (a shared
        // queue) it has no signal and returns 0, which effectively disables dialing — use Redis.
        var mode = (campaign.AssignedMode ?? "agents").ToLowerInvariant();
        return Task.FromResult(mode == "queue" ? 0 : GetFreeAgents(campaign.Id));
    }

    public Task<int> GetFreeAgentsForQueueAsync(string tenantId, string queueName, CancellationToken ct)
    {
        // Try to reverse the convention t_{tenant}__qc_{campaign} to reuse the map.
        var idx = queueName.IndexOf("__qc_", StringComparison.Ordinal);
        if (idx < 0) return Task.FromResult(0);
        var campaignId = queueName[(idx + 5)..];
        return Task.FromResult(GetFreeAgents(campaignId));
    }

    public Task HandleAsync(AmiEventEnvelope env, CancellationToken ct)
    {
        var queue = env.Raw.GetValueOrDefault("Queue");
        var campaignId = ExtractCampaignId(queue);
        if (campaignId is null) return Task.CompletedTask;

        var memberKey = env.Raw.GetValueOrDefault("Interface")
                        ?? env.Raw.GetValueOrDefault("MemberName")
                        ?? env.Raw.GetValueOrDefault("StateInterface");
        if (string.IsNullOrWhiteSpace(memberKey)) return Task.CompletedTask;

        var members = _queues.GetOrAdd(campaignId, _ => new ConcurrentDictionary<string, AgentState>());
        var state = members.GetOrAdd(memberKey, _ => new AgentState());

        switch (env.Event)
        {
            case "QueueMember":
            case "QueueMemberStatus":
            case "QueueMemberAdded":
                int.TryParse(env.Raw.GetValueOrDefault("Status"), out var s);
                state.Status = s == 0 ? state.Status : s;
                state.Paused = env.Raw.GetValueOrDefault("Paused") == "1";
                break;

            case "QueueMemberPause":
                state.Paused = env.Raw.GetValueOrDefault("Paused") == "1";
                break;

            case "QueueMemberRemoved":
                members.TryRemove(memberKey, out _);
                break;

            case "AgentConnect":
                state.OnCall = true;
                break;

            case "AgentComplete":
                state.OnCall = false;
                break;
        }
        return Task.CompletedTask;
    }

    private static string? ExtractCampaignId(string? queue)
    {
        if (string.IsNullOrWhiteSpace(queue)) return null;
        var idx = queue.IndexOf("__qc_", StringComparison.Ordinal);
        if (idx < 0) return null;
        var id = queue.Substring(idx + 5);
        return string.IsNullOrWhiteSpace(id) ? null : id;
    }
}
