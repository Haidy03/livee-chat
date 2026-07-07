namespace VoiceFlow.FreeSwitchXmlCurl.Services;

public interface ITemplateResolver
{
    /// <summary>
    /// Replaces ${name} placeholders using the provided variables.
    /// Unknown placeholders are preserved verbatim so FreeSWITCH can resolve them
    /// at runtime (e.g. ${ivr_digit}).
    /// </summary>
    string Resolve(string? template, IReadOnlyDictionary<string, string> variables);
}
