using VoiceFlow.Contracts.Live;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface ILiveSnapshotService
{
    Task<Result<LiveSnapshot>> GetSnapshotAsync(string tenantId, CancellationToken cancellationToken = default);
}
