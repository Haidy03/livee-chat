
using VoiceFlow.Api.UserMaps.Requests;
using VoiceFlow.Contracts.UserMaps.Responses;

namespace VoiceFlow.Application.Interfaces;

public interface IUsersMapService
{
    Task<UsersMapSnapshotResponse> GetSnapshotAsync(string tenantId, CancellationToken ct);
    Task<bool> ActionAsync(string tenantId, string callId, string kind, TransferRequest? transfer, CancellationToken ct);
}
