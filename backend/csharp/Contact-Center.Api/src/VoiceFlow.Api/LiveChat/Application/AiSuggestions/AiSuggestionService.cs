using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Config;
using VoiceFlow.Api.LiveChat.Domain;
using VoiceFlow.Api.LiveChat.Infrastructure.Ai;

namespace VoiceFlow.Api.LiveChat.Application.AiSuggestions;

public sealed class AiSuggestDisabledException : Exception
{
    public AiSuggestDisabledException() : base("AI Suggest is currently unavailable. Please try again later.") { }
}

public sealed class AiSuggestForbiddenException : Exception
{
    public AiSuggestForbiddenException(string message) : base(message) { }
}

public sealed class AiSuggestNotFoundException : Exception
{
    public AiSuggestNotFoundException(string message) : base(message) { }
}

public interface IAiSuggestionService
{
    Task<AiSuggestionResponse> GenerateAsync(string projectId, string agentId, AiSuggestionRequest request, CancellationToken ct);
    Task MarkUsedAsync(string projectId, string agentId, string suggestionId, string usedText, string? sentMessageId, CancellationToken ct);
    Task AddFeedbackAsync(string projectId, string agentId, AiSuggestionFeedbackRequest request, CancellationToken ct);
    Task<IReadOnlyList<AiSuggestion>> ListByRoomAsync(string projectId, string roomId, int limit, CancellationToken ct);
}

public sealed class AiSuggestionService : IAiSuggestionService
{
    private readonly IAiSuggestionContextBuilder _contextBuilder;
    private readonly IAiSuggestionPromptBuilder _promptBuilder;
    private readonly ILlmClient _llm;
    private readonly IAiSuggestionRepository _repo;
    private readonly AiSuggestRateLimiter _rateLimiter;
    private readonly IOptions<AiSuggestOptions> _options;
    private readonly ILogger<AiSuggestionService> _logger;

    public AiSuggestionService(
        IAiSuggestionContextBuilder contextBuilder,
        IAiSuggestionPromptBuilder promptBuilder,
        ILlmClient llm,
        IAiSuggestionRepository repo,
        AiSuggestRateLimiter rateLimiter,
        IOptions<AiSuggestOptions> options,
        ILogger<AiSuggestionService> logger)
    {
        _contextBuilder = contextBuilder;
        _promptBuilder = promptBuilder;
        _llm = llm;
        _repo = repo;
        _rateLimiter = rateLimiter;
        _options = options;
        _logger = logger;
    }

    public async Task<AiSuggestionResponse> GenerateAsync(string projectId, string agentId, AiSuggestionRequest request, CancellationToken ct)
    {
        if (!_options.Value.Enabled) throw new AiSuggestDisabledException();
        if (string.IsNullOrWhiteSpace(request.RoomId)) throw new AiSuggestForbiddenException("roomId is required.");

        _rateLimiter.Check(projectId, agentId);

        var ctx = await _contextBuilder.BuildAsync(projectId, agentId, request.RoomId, ct);
        if (ctx is null) throw new AiSuggestForbiddenException("Room not found or agent not assigned.");

        var entity = new AiSuggestion
        {
            projectId = projectId,
            roomId = request.RoomId,
            conversationId = ctx.ConversationId,
            requestedByAgentId = agentId,
            type = request.Type,
            status = AiSuggestionStatus.Pending,
            agentDraft = request.AgentDraft,
            targetLanguage = request.TargetLanguage,
            promptVersion = _promptBuilder.PromptVersion,
        };

        try
        {
            var userPrompt = _promptBuilder.BuildUserPrompt(request.Type, ctx, request.AgentDraft, request.TargetLanguage);
            var raw = await _llm.GenerateJsonAsync(_promptBuilder.SystemPrompt, userPrompt, ct);
            ApplyLlmJson(entity, raw);
            entity.status = AiSuggestionStatus.Completed;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI Suggest generation failed for room {RoomId}", request.RoomId);
            entity.status = AiSuggestionStatus.Failed;
            entity.errorMessage = ex.Message;
            entity.warning = "AI Suggest is temporarily unavailable. Please try again.";
        }

        await _repo.CreateAsync(entity, ct);

        return new AiSuggestionResponse
        {
            SuggestionId = entity._id,
            Type = entity.type,
            SuggestedReplies = entity.suggestedReplies,
            Summary = entity.summary,
            DetectedIntent = entity.detectedIntent,
            SuggestedTags = entity.suggestedTags,
            NextActions = entity.nextActions,
            Confidence = entity.confidence,
            Warning = entity.warning,
        };
    }

    public async Task MarkUsedAsync(string projectId, string agentId, string suggestionId, string usedText, string? sentMessageId, CancellationToken ct)
    {
        var ok = await _repo.MarkUsedAsync(projectId, suggestionId, agentId, usedText, sentMessageId, ct);
        if (!ok) throw new AiSuggestNotFoundException("Suggestion not found.");
    }

    public async Task AddFeedbackAsync(string projectId, string agentId, AiSuggestionFeedbackRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SuggestionId)) throw new AiSuggestNotFoundException("SuggestionId is required.");
        var ok = await _repo.AddFeedbackAsync(projectId, request.SuggestionId, agentId, request.Feedback, request.Comment, ct);
        if (!ok) throw new AiSuggestNotFoundException("Suggestion not found.");
    }

    public Task<IReadOnlyList<AiSuggestion>> ListByRoomAsync(string projectId, string roomId, int limit, CancellationToken ct)
        => _repo.ListByRoomAsync(projectId, roomId, Math.Clamp(limit, 1, 200), ct);

    private static void ApplyLlmJson(AiSuggestion e, string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (root.TryGetProperty("suggestedReplies", out var arr) && arr.ValueKind == JsonValueKind.Array)
            e.suggestedReplies = arr.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList();

        if (root.TryGetProperty("summary", out var s) && s.ValueKind == JsonValueKind.String)
            e.summary = s.GetString();

        if (root.TryGetProperty("detectedIntent", out var di) && di.ValueKind == JsonValueKind.String)
            e.detectedIntent = di.GetString();

        if (root.TryGetProperty("suggestedTags", out var tags) && tags.ValueKind == JsonValueKind.Array)
            e.suggestedTags = tags.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList();

        if (root.TryGetProperty("nextActions", out var na) && na.ValueKind == JsonValueKind.Array)
            e.nextActions = na.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList();

        if (root.TryGetProperty("confidence", out var c) && (c.ValueKind == JsonValueKind.Number))
            e.confidence = (decimal)c.GetDouble();

        if (root.TryGetProperty("warning", out var w) && w.ValueKind == JsonValueKind.String)
            e.warning = w.GetString();
    }
}
