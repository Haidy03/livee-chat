namespace VoiceFlow.Api.LiveChat.Application;

/// <summary>
/// Normalizes free-form language tokens (e.g. "English", "eng", "EN") to a
/// canonical ISO-639-1 two-letter code so agent presence languages match
/// client request `lang` values regardless of how each side spells them.
/// Unknown values fall back to their lowercased trimmed form.
/// </summary>
internal static class LanguageNormalizer
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["en"] = "en", ["eng"] = "en", ["english"] = "en",
        ["ar"] = "ar", ["ara"] = "ar", ["arabic"] = "ar",
        ["عربي"] = "ar", ["العربية"] = "ar",
        ["fr"] = "fr", ["fra"] = "fr", ["fre"] = "fr", ["french"] = "fr",
        ["es"] = "es", ["spa"] = "es", ["spanish"] = "es",
        ["de"] = "de", ["ger"] = "de", ["deu"] = "de", ["german"] = "de",
        ["tr"] = "tr", ["tur"] = "tr", ["turkish"] = "tr",
    };

    public static string Normalize(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return string.Empty;
        var trimmed = lang.Trim();
        return Map.TryGetValue(trimmed, out var canonical) ? canonical : trimmed.ToLowerInvariant();
    }

    public static bool Matches(IEnumerable<string> agentLangs, string requestLang)
    {
        if (string.IsNullOrWhiteSpace(requestLang)) return true;
        var target = Normalize(requestLang);
        foreach (var a in agentLangs)
        {
            if (Normalize(a) == target) return true;
        }
        return false;
    }
}
