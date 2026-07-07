namespace VoiceFlow.Api.LiveChat.Infrastructure.Ai;

public interface ILlmClient
{
    Task<string> GenerateJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}
