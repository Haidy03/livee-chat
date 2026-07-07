using Microsoft.Extensions.Logging;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Infrastructure.ExternalServices;

public sealed class AiGatewayService : IAiGatewayService
{
    private readonly ILogger<AiGatewayService> _logger;

    public AiGatewayService(ILogger<AiGatewayService> logger)
    {
        _logger = logger;
    }

    public Task<string?> TranscribeAsync(string audioUrl, string language, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI: transcribing audio at {Url}", audioUrl);
        return Task.FromResult<string?>(null);
    }

    public Task<string?> SummarizeAsync(string transcript, string targetLanguage, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI: summarizing transcript ({Length} chars) into {Language}", transcript.Length, targetLanguage);
        return Task.FromResult<string?>(null);
    }

    public Task<string?> AnalyzeSentimentAsync(string text, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI: analyzing sentiment");
        return Task.FromResult<string?>("Neutral");
    }

    public Task<string?> TranslateAsync(string text, string targetLanguage, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI: translating to {Language}", targetLanguage);
        return Task.FromResult<string?>(text);
    }
}
