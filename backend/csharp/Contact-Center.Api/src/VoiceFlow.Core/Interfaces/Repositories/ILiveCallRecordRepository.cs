using VoiceFlow.Reports.Core.Entities;

namespace VoiceFlow.Core.Interfaces.Repositories;

public interface ILiveCallRecordRepository
{
    Task AddAsync(LiveCall record, CancellationToken ct);

    Task<(IReadOnlyList<LiveCall> Items, long Total)> SearchAsync(
        string tenantId,
        string? search,
        DateTime? from,
        DateTime? to,
        string? direction,
        string? channel,
        string? finalState,
        int skip,
        int take,
        CancellationToken ct);
}
