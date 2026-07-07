using VoiceFlow.FreeSwitchXmlCurl.Models;

namespace VoiceFlow.FreeSwitchXmlCurl.Services;

public interface IVoicemailService
{
    Task<string> RecordAsync(VoicemailMessage message, CancellationToken ct = default);
}
