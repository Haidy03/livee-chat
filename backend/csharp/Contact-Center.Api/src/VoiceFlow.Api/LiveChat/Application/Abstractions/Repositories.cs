using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application.Abstractions;

public interface IClientRequestRepository
{
    Task InsertAsync(ClientRequest req, CancellationToken ct = default);
    Task<ClientRequest?> GetAsync(string id, CancellationToken ct = default);
    Task<ClientRequest?> GetAsync(string id, IClientSessionHandle session, CancellationToken ct = default);
    Task<ClientRequest?> TryLockAsync(string id, CancellationToken ct = default);
    Task UnlockAsync(string id, string excludedAgentId, CancellationToken ct = default);
    Task SetOfflineAsync(string id, CancellationToken ct = default);
    Task<long> DeleteAsync(string id, IClientSessionHandle? session = null, CancellationToken ct = default);
    Task<List<ClientRequest>> GetUnlockedPendingAsync(CancellationToken ct = default);
    Task<List<ClientRequest>> GetStaleOfflineAsync(DateTime olderThan, CancellationToken ct = default);
}

public interface IRoomRepository
{
    Task InsertAsync(Room c, IClientSessionHandle? session = null, CancellationToken ct = default);
    Task<Room?> GetAsync(string id, CancellationToken ct = default);
    Task<Room?> GetActiveByChannelContactAsync(string channel, string contactId, CancellationToken ct = default);
    Task AppendMessageAsync(string roomId, Message message, CancellationToken ct = default);
    Task CloseAsync(string id, string typeOfClose, CancellationToken ct = default);
    Task<List<Room>> GetActiveByAgentAsync(string agentId, CancellationToken ct = default);
    Task ReassignAgentAsync(string roomId, string agentId, CancellationToken ct = default);
    Task RequeueToGroupAsync(string roomId, string groupId, CancellationToken ct = default);
}

public interface IAgentRepository
{
    Task<Agent?> GetAsync(string id, CancellationToken ct = default);
}
