using VoiceFlow.Contracts.Campaigns;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface ICampaignService
{
    Task<Result<IEnumerable<CampaignResponse>>> GetAllAsync(string tenantId, CancellationToken ct = default);
    Task<Result<CampaignResponse>> GetByIdAsync(string id, string tenantId, CancellationToken ct = default);
    Task<Result<CampaignResponse>> CreateAsync(string tenantId, CreateCampaignRequest request, CancellationToken ct = default);
    Task<Result<CampaignResponse>> UpdateAsync(string id, string tenantId, UpdateCampaignRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, string tenantId, CancellationToken ct = default);
    Task<Result<CampaignResponse>> SetStatusAsync(string id, string tenantId, SetCampaignStatusRequest request, CancellationToken ct = default);

    // -------- Targets (paginated, lives in campaign_targets collection) --------
    Task<Result<PagedResponse<CampaignContactDto>>> ListTargetsAsync(string id, string tenantId, ListCampaignTargetsRequest request, CancellationToken ct = default);
    Task<Result<int>> AddTargetsAsync(string id, string tenantId, AddCampaignContactsRequest request, CancellationToken ct = default);
    Task<Result> RemoveTargetAsync(string id, string tenantId, string targetId, CancellationToken ct = default);
    Task<Result<CampaignContactDto>> UpdateTargetStatusAsync(string id, string tenantId, string targetId, UpdateCampaignContactStatusRequest request, CancellationToken ct = default);

    // -------- Activity (paginated, lives in campaign_activity collection) --------
    Task<Result<PagedResponse<CampaignActivityEntryDto>>> ListActivityAsync(string id, string tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<CampaignActivityEntryDto>> AddActivityAsync(string id, string tenantId, AddCampaignActivityRequest request, CancellationToken ct = default);

    // -------- Received calls (paginated, lives in campaign_received_calls collection) --------
    Task<Result<PagedResponse<CampaignReceivedCallDto>>> ListReceivedCallsAsync(string id, string tenantId, int page, int pageSize, CancellationToken ct = default);
    Task<Result<CampaignReceivedCallDto>> AddReceivedCallAsync(string id, string tenantId, AddCampaignReceivedCallRequest request, CancellationToken ct = default);
}
