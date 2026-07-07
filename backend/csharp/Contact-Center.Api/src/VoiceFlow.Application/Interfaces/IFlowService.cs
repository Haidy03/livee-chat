using VoiceFlow.Contracts.Flows;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IFlowService
{
    Task<Result<IEnumerable<FlowResponse>>> GetFlowsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<FlowResponse>> GetFlowAsync(string flowId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<FlowResponse>> CreateFlowAsync(string tenantId, string userId, CreateFlowRequest request, CancellationToken cancellationToken = default);
    Task<Result<FlowResponse>> UpdateFlowAsync(string flowId, string tenantId, UpdateFlowRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteFlowAsync(string flowId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<FlowValidationResponse>> ValidateFlowAsync(string flowId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<FlowResponse>> PublishFlowAsync(string flowId, string tenantId, PublishFlowRequest request, CancellationToken cancellationToken = default);
    Task<Result<FlowExportResponse>> ExportFlowAsync(string flowId, string tenantId, CancellationToken cancellationToken = default);
}
