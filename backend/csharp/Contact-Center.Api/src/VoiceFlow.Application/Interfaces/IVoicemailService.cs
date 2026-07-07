using VoiceFlow.Contracts.Voicemail;

namespace VoiceFlow.Application.Interfaces;

/// <summary>
/// Voicemail ingest (from the Asterisk dialplan webhook) + the agent inbox. Ownership is a
/// queue/group/agent; the audience is resolved at read time from queue/group membership.
/// </summary>
public interface IVoicemailService
{
    /// <summary>Inbox for one agent: voicemails owned by the queues/groups they belong to (+ their own).</summary>
    Task<IReadOnlyList<VoicemailResponse>> ListForAgentAsync(
        string tenantId, string agentId, string? status, CancellationToken cancellationToken = default);

    /// <summary>Per-owner unread ("new") counts for the queues/groups the agent belongs to (badge source).</summary>
    Task<IReadOnlyDictionary<string, int>> UnreadCountsForAgentAsync(
        string tenantId, string agentId, CancellationToken cancellationToken = default);

    Task<bool> ClaimAsync(string tenantId, string agentId, string id, CancellationToken cancellationToken = default);

    Task<bool> ResolveAsync(string tenantId, string agentId, string id, CancellationToken cancellationToken = default);
}
