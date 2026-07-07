using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface IVoicemailRepository : IRepository<Voicemail>
{
    /// <summary>
    /// Worker write-back after S3 upload + optional transcription. Sets s3Url and (when
    /// analysis is present) transcript/summary/sentiment. Returns false if no doc matched.
    /// </summary>
    Task<bool> SetProcessingResultAsync(
        string id,
        string s3Url,
        CallAnalysisResult? analysis,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inbox listing: voicemails whose owner is one of the given owner ids (the queues/groups
    /// the requesting agent belongs to, plus their own agent id), optionally filtered by status.
    /// </summary>
    Task<IReadOnlyList<Voicemail>> ListForOwnersAsync(
        string tenantId,
        IEnumerable<string> ownerIds,
        string? status,
        CancellationToken cancellationToken = default);

    /// <summary>Per-owner unread ("new") counts for the given owner ids (badge source).</summary>
    Task<IReadOnlyDictionary<string, int>> CountNewByOwnerAsync(
        string tenantId,
        IEnumerable<string> ownerIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimistic claim: transitions status new → claimed only if still new. Returns false if
    /// already claimed/resolved (someone won the race).
    /// </summary>
    Task<bool> TryClaimAsync(
        string tenantId,
        string id,
        string agentId,
        CancellationToken cancellationToken = default);

    /// <summary>Mark a voicemail resolved/done.</summary>
    Task<bool> ResolveAsync(
        string tenantId,
        string id,
        string agentId,
        CancellationToken cancellationToken = default);
}
