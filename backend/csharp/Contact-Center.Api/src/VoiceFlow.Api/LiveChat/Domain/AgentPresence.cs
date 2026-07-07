namespace VoiceFlow.Api.LiveChat.Domain;

// Hydrated from Redis hash — not persisted.
public class AgentPresence
{
    public string AgentId { get; set; } = string.Empty;
    public AgentStatus Status { get; set; } = AgentStatus.Offline;
    public int ActiveChats { get; set; }
    public int MaxConcurrency { get; set; } = 4;
    public DateTime? LastAssignedAt { get; set; }
    public List<string> DepartmentIds { get; set; } = new();
    public List<string> Languages { get; set; } = new();

    public bool HasCapacity => Status == AgentStatus.Available && ActiveChats < MaxConcurrency;
}
