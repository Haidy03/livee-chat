using VoiceFlow.FreeSwitchXmlCurl.Models;

namespace VoiceFlow.FreeSwitchXmlCurl.Services;

public interface IXmlDialplanRenderer
{
    string RenderMatched(
        string context,
        string destinationNumber,
        DialplanDocument document,
        DialplanEntry entry,
        IReadOnlyList<DialplanAction> resolvedActions);

    string RenderInvalidRoute(string context, string destinationNumber);

    string RenderNotFound();
}
