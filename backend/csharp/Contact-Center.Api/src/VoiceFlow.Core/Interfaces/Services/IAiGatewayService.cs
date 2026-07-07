namespace VoiceFlow.Core.Interfaces.Services;

public interface IAiGatewayService
{
    Task<string?> TranscribeAsync(string audioUrl, string language, CancellationToken cancellationToken = default);
    Task<string?> SummarizeAsync(string transcript, string targetLanguage, CancellationToken cancellationToken = default);
    Task<string?> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default);
    Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default);
}
