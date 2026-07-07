using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Enums;
using VoiceFlow.Core.Models;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface ICallRepository : IRepository<Call>
{
    Task<Call?> GetByTenantAndExternalCallIdAsync(string tenantId, string externalCallId, CancellationToken cancellationToken = default);

    Task<(IEnumerable<Call> Items, long TotalCount)> SearchAsync(
        string tenantId,
        CallDirection? direction,
        CallStatus? status,
        DateTime? from,
        DateTime? to,
        string? caller,
        IEnumerable<string>? tagIds,
        string? userId,
        bool? softphoneOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<Call> Items, long TotalCount)> AdvancedSearchAsync(
        string tenantId,
        CallAdvancedSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctHangupCausesAsync(string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Softphone sessions still in progress (no end time, external SIP call id set).</summary>
    Task<IReadOnlyList<Call>> GetActiveCallsForTenantAsync(string tenantId, CancellationToken cancellationToken = default);

    public Task<bool> SetRecordingUrlAsync(
       string id, string recordingUrl, CallAnalysisResult? analysis = null, CancellationToken cancellationToken = default);

    public Task<int> CloseNonTerminatedCallsAsync();

}
