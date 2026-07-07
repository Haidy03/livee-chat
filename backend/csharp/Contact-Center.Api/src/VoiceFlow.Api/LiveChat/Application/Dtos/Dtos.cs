using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application.Dtos;

public class NewContact
{
    public string Channel { get; set; } = string.Empty;
    public string AgentChannel { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string ChatbotId { get; set; } = string.Empty;
    public string ContactId { get; set; } = string.Empty;
    public string Lang { get; set; } = "en";
    public string ConnectionId { get; set; } = string.Empty;
    public string ClientInfo { get; set; } = string.Empty;
    public DepartmentInfo Department { get; set; } = new();
}

public class StartChatPayload : NewContact { }
