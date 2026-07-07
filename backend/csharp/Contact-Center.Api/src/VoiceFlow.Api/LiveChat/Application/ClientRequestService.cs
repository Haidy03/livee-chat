using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Application.Dtos;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application;

public sealed class ClientRequestService
{
    private readonly IClientRequestRepository _repo;
    private readonly RoutingEngine _routing;

    public ClientRequestService(IClientRequestRepository repo, RoutingEngine routing)
    {
        _repo = repo;
        _routing = routing;
    }

    public async Task<string> CreateRequestAsync(NewContact contact, CancellationToken ct = default)
    {
        var dept = NormalizeDepartment(contact.Department);
        var req = new ClientRequest
        {
            userId = contact.UserId,
            channel = contact.Channel,
            agentChannel = contact.AgentChannel,
            projectId = contact.ProjectId,
            chatbotId = contact.ChatbotId,
            contact_Id = contact.ContactId,
            department = dept,
            lang = contact.Lang,
            connectionId = contact.ConnectionId ?? string.Empty,
            status = new ConnectionStatus { state = "online", timeStamp = DateTime.UtcNow },
            created = DateTime.UtcNow,
            upadtedAt = DateTime.UtcNow,
            execludedAgentId = string.Empty,
            clientInfo = contact.ClientInfo ?? string.Empty,
            typeOfClose = string.Empty,
            locked = false,
            comment = string.Empty,
            requestCount = 0,
        };
        await _repo.InsertAsync(req, ct);
        await _routing.TryDispatchAsync(req._id, ct);
        return req._id;
    }

    public Task MarkOfflineAsync(string requestId, CancellationToken ct = default) =>
        _repo.SetOfflineAsync(requestId, ct);

    private static DepartmentInfo NormalizeDepartment(DepartmentInfo? dept)
    {
        if (dept is null) return new DepartmentInfo { id = null!, name = string.Empty };
        var id = dept.id;
        var isValidObjectId = !string.IsNullOrWhiteSpace(id)
            && id.Length == 24
            && id.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'));
        return new DepartmentInfo
        {
            id = isValidObjectId ? id : null!,
            name = dept.name ?? string.Empty,
        };
    }
}
