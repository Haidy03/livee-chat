using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Common;
using VoiceFlow.Contracts.Visitors;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Interfaces.Repositories;
using VoiceFlow.Reports.Core.Entities;

namespace VoiceFlow.Application.Services.Visitors;

public sealed class VisitorsService : IVisitorsService
{
    private readonly ILiveCallRecordRepository _repo;

    public VisitorsService(ILiveCallRecordRepository repo)
    {
        _repo = repo;
    }

    public async Task<Result<PagedResponse<VisitorResponse>>> SearchAsync(
        string tenantId,
        VisitorsQuery query,
        CancellationToken ct = default)
    {
        var (items, total) = await _repo.SearchAsync(
            tenantId,
            query.Search,
            query.From,
            query.To,
            query.Direction,
            query.Channel,
            query.FinalState,
            query.Skip,
            query.PageSize,
            ct);

        var mapped = items.Select(Map).ToList().AsReadOnly();
        return PagedResponse<VisitorResponse>.Create(mapped, query.Page, query.PageSize, total);
    }

    private static VisitorResponse Map(LiveCall c) => new()
    {
        Id = c.Id,
        CallId = c.CallId,
        Name = c.Name,
        MaskedNumber = c.MaskedNumber,
        Color = c.Color,
        Direction = c.Direction,
        Channel = c.Channel,
        FinalState = c.FinalState,
        Reason = c.Reason,
        CallStartedAt = c.CallStartedAt,
        EndedAt = c.EndedAt,
        DurationSec = c.DurationSec,
        AgentId = c.Agent?.Id,
        AgentName = c.Agent?.Name,
        FlowId = c.FlowId,
        NodeLabel = c.NodeLabel,
        IvrChoice = c.IvrChoice,
        Intent = c.Intent,
        Tags = c.Tags,
    };
}
