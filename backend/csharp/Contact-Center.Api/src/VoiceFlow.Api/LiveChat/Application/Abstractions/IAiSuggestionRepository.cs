using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application.Abstractions;

public interface IAiSuggestionRepository
{
    Task CreateAsync(AiSuggestion s, CancellationToken ct = default);
    Task<AiSuggestion?> GetByIdAsync(string projectId, string id, CancellationToken ct = default);
    Task<IReadOnlyList<AiSuggestion>> ListByRoomAsync(string projectId, string roomId, int limit, CancellationToken ct = default);
    Task<bool> MarkUsedAsync(string projectId, string id, string agentId, string usedText, string? sentMessageId, CancellationToken ct = default);
    Task<bool> AddFeedbackAsync(string projectId, string id, string agentId, AiSuggestionFeedback feedback, string? comment, CancellationToken ct = default);
}
