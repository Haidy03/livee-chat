using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application.Abstractions;

public interface IPresenceStore
{
    Task HydrateAsync(Agent agent, string connectionId);
    Task AddConnectionAsync(string agentId, string connectionId);
    Task RemoveConnectionAsync(string agentId, string connectionId);
    Task<bool> HasConnectionsAsync(string agentId);
    Task<string?> GetAnyConnectionAsync(string agentId);
    Task SetStatusAsync(string agentId, AgentStatus status);
    Task<AgentPresence?> GetAsync(string agentId);
    Task<int> IncrementLoadAsync(string agentId);
    Task<int> DecrementLoadAsync(string agentId);
    Task TouchLastAssignedAsync(string agentId);
    Task<string?> SelectAgentAsync(string departmentId, string lang, IEnumerable<string> excludeAgentIds);
}

public interface IOfferTimeoutStore
{
    Task ArmAsync(string requestId, string agentId, TimeSpan ttl);
    Task CancelAsync(string requestId);
    Task<List<(string RequestId, string AgentId)>> PopExpiredAsync();
}
