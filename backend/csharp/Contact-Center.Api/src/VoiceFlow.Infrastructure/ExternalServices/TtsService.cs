using Microsoft.Extensions.Logging;
using VoiceFlow.Core.Interfaces.Services;

namespace VoiceFlow.Infrastructure.ExternalServices;

public sealed class TtsService : ITtsService
{
    private readonly ILogger<TtsService> _logger;

    public TtsService(ILogger<TtsService> logger) => _logger = logger;

    public Task<Stream> SynthesizeAsync(string text, string language, string voice, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("TTS: synthesizing text ({Length} chars) in {Language}", text.Length, language);
        Stream empty = new MemoryStream();
        return Task.FromResult(empty);
    }
}
