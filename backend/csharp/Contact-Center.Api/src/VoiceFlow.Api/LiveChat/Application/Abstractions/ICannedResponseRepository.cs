using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application.Abstractions;

public interface ICannedResponseRepository
{
    Task<IReadOnlyList<CannedResponse>> GetAllByProjectAsync(string projectId, CancellationToken ct = default);
    Task<CannedResponse?> GetByIdAsync(string projectId, string id, CancellationToken ct = default);
    Task CreateAsync(CannedResponse entity, CancellationToken ct = default);
    Task<CannedResponse?> UpdateAsync(string projectId, string id, CannedResponse update, CancellationToken ct = default);
    Task<bool> DeleteAsync(string projectId, string id, CancellationToken ct = default);
}
