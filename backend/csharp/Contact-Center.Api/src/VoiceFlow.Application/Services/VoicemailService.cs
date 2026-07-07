using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Voicemail;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class VoicemailService : IVoicemailService
{
    private readonly IVoicemailRepository _voicemails;
    private readonly IQueueRepository _queues;
    private readonly IGroupRepository _groups;

    public VoicemailService(
        IVoicemailRepository voicemails,
        IQueueRepository queues,
        IGroupRepository groups)
    {
        _voicemails = voicemails;
        _queues = queues;
        _groups = groups;
    }

    public async Task<IReadOnlyList<VoicemailResponse>> ListForAgentAsync(
        string tenantId, string agentId, string? status, CancellationToken cancellationToken = default)
    {
        var ownerIds = await ResolveOwnerIdsForAgentAsync(tenantId, agentId, cancellationToken);
        if (ownerIds.Count == 0) return [];

        var items = await _voicemails.ListForOwnersAsync(tenantId, ownerIds, status, cancellationToken);
        return items.Select(Map).ToList();
    }

    public async Task<IReadOnlyDictionary<string, int>> UnreadCountsForAgentAsync(
        string tenantId, string agentId, CancellationToken cancellationToken = default)
    {
        var ownerIds = await ResolveOwnerIdsForAgentAsync(tenantId, agentId, cancellationToken);
        if (ownerIds.Count == 0) return new Dictionary<string, int>();
        return await _voicemails.CountNewByOwnerAsync(tenantId, ownerIds, cancellationToken);
    }

    public Task<bool> ClaimAsync(string tenantId, string agentId, string id, CancellationToken cancellationToken = default)
        => _voicemails.TryClaimAsync(tenantId, id, agentId, cancellationToken);

    public Task<bool> ResolveAsync(string tenantId, string agentId, string id, CancellationToken cancellationToken = default)
        => _voicemails.ResolveAsync(tenantId, id, agentId, cancellationToken);

    /// <summary>
    /// The queues and groups the agent belongs to, plus their own agent id (for direct-to-agent
    /// voicemail). This set drives what the agent is entitled to see.
    /// </summary>
    private async Task<List<string>> ResolveOwnerIdsForAgentAsync(
        string tenantId, string agentId, CancellationToken cancellationToken)
    {
        var owners = new HashSet<string> { agentId };

        var queues = await _queues.GetByTenantAsync(tenantId, cancellationToken);
        foreach (var q in queues)
        {
            if (q.Members.Any(m => m.AgentId == agentId && m.Enabled))
                owners.Add(q.Id);
        }

        var groups = await _groups.GetByTenantAsync(tenantId, cancellationToken);
        foreach (var g in groups)
        {
            if (g.Members.Contains(agentId))
                owners.Add(g.Id);
        }

        return owners.ToList();
    }

    private static VoicemailResponse Map(Voicemail v) => new()
    {
        Id = v.Id,
        OwnerType = v.OwnerType,
        OwnerId = v.OwnerId,
        CallerIdNumber = v.CallerIdNumber,
        DestinationNumber = v.DestinationNumber,
        DurationSeconds = v.DurationSeconds,
        Timestamp = v.Timestamp,
        S3Url = v.S3Url,
        Transcript = v.Transcript,
        Summary = v.Summary,
        Sentiment = v.Sentiment,
        TranscriptionRequested = v.TranscriptionRequested,
        Status = v.Status,
        ClaimedBy = v.ClaimedBy,
        ClaimedAt = v.ClaimedAt,
        ResolvedBy = v.ResolvedBy,
        ResolvedAt = v.ResolvedAt,
    };
}
