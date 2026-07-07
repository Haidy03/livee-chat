using Microsoft.Extensions.Options;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Config;
using VoiceFlow.Api.LiveChat.Domain;

namespace VoiceFlow.Api.LiveChat.Application.AiSuggestions;

public interface IAiSuggestionContextBuilder
{
    Task<AiConversationContext?> BuildAsync(string projectId, string agentId, string roomId, CancellationToken ct);
}

/// <summary>
/// Loads a room, checks tenant + agent ownership, and produces a trimmed
/// message context suitable for LLM prompting. Attachments and secrets are
/// stripped; history is capped to <see cref="AiSuggestOptions.MaxMessages"/>.
/// </summary>
public sealed class AiSuggestionContextBuilder : IAiSuggestionContextBuilder
{
    private readonly IRoomRepository _rooms;
    private readonly IOptions<AiSuggestOptions> _options;

    public AiSuggestionContextBuilder(IRoomRepository rooms, IOptions<AiSuggestOptions> options)
    {
        _rooms = rooms;
        _options = options;
    }

    public async Task<AiConversationContext?> BuildAsync(string projectId, string agentId, string roomId, CancellationToken ct)
    {
        var room = await _rooms.GetAsync(roomId, ct);
        if (room is null) return null;
        if (!string.Equals(room.projectId, projectId, StringComparison.Ordinal)) return null;
        if (!string.Equals(room.agentId, agentId, StringComparison.Ordinal)) return null;

        var max = Math.Max(1, _options.Value.MaxMessages);
        var messages = (room.Messages ?? new List<Message>())
            .Where(m => !string.IsNullOrWhiteSpace(m.Text))
            .OrderByDescending(m => m.Timestamp)
            .Take(max)
            .OrderBy(m => m.Timestamp)
            .Select(m => new AiMessageContextItem
            {
                SenderType = m.SenderType.ToString(),
                SenderName = null,
                Text = m.Text,
                SentAtUtc = m.Timestamp,
            })
            .ToList();

        return new AiConversationContext
        {
            ProjectId = projectId,
            RoomId = room._id,
            ConversationId = room.conversationId.ToString(),
            CustomerName = room.clientId,
            Channel = string.IsNullOrEmpty(room.channel) ? room.agentChannel : room.channel,
            Lang = room.lang,
            Messages = messages,
            CurrentAgentName = room.agentName,
            DepartmentName = room.department?.name,
        };
    }
}
