using VoiceFlow.Api.Calls;
using VoiceFlow.Contracts.Calls;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface ICallService
{
    Task<Result<PagedResponse<CallResponse>>> SearchCallsAsync(string tenantId, CallSearchRequest request, CancellationToken cancellationToken = default);
    Task<Result<CallSearchResponse>> AdvancedSearchCallsAsync(string tenantId, AdvancedCallSearchRequest request, CancellationToken cancellationToken = default);
    Task<Result<CallFilterOptions>> GetCallFilterOptionsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<CallResponse>> GetCallAsync(string callId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<CallResponse>> CreateCallAsync(string tenantId, string userId, CreateCallRequest request, CancellationToken cancellationToken = default);
    Task<Result<CallResponse>> UpdateCallAsync(string callId, string tenantId, UpdateCallRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteCallAsync(string callId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<SignedUrlResponse>> GetRecordingUrlAsync(string callId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<CallResponse>> GenerateSummaryAsync(string callId, string tenantId, GenerateSummaryRequest request, CancellationToken cancellationToken = default);
    Task<Result<CallResponse>> TranslateSummaryAsync(string callId, string tenantId, TranslateSummaryRequest request, CancellationToken cancellationToken = default);
    Task<Result<CallResponse>> UpsertSoftphoneCallAsync(string tenantId, string userId, SoftphoneCallUpsertRequest request, CancellationToken cancellationToken = default);

    Task<WrapUpCallResponse> SaveWrapUpAsync(string tenantId, WrapUpCallRequest request, CancellationToken ct);

}
