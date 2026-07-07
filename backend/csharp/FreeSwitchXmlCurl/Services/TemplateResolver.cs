using System.Text.RegularExpressions;

namespace VoiceFlow.FreeSwitchXmlCurl.Services;

public sealed class TemplateResolver : ITemplateResolver
{
    private static readonly Regex Placeholder =
        new(@"\$\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled);

    public string Resolve(string? template, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;
        return Placeholder.Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            return variables.TryGetValue(key, out var v) ? v : m.Value;
        });
    }
}
