using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Visitors;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IVisitorsService
{
    Task<Result<PagedResponse<VisitorResponse>>> SearchAsync(
        string tenantId,
        VisitorsQuery query,
        CancellationToken ct = default);
}
