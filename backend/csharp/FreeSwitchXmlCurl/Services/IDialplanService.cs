namespace VoiceFlow.FreeSwitchXmlCurl.Services;

public interface IDialplanService
{
    Task<string> HandleAsync(IReadOnlyDictionary<string, string> parameters, CancellationToken ct = default);
}
