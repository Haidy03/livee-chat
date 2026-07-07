namespace VoiceFlow.Core.Interfaces.Services;

public interface ITtsService
{
    Task<Stream> SynthesizeAsync(string text, string language, string voice, CancellationToken cancellationToken = default);
}
