using System.Text;
using System.Xml;
using System.Xml.Linq;
using VoiceFlow.FreeSwitchXmlCurl.Models;

namespace VoiceFlow.FreeSwitchXmlCurl.Services;

public sealed class XmlDialplanRenderer : IXmlDialplanRenderer
{
    public string RenderMatched(
        string context,
        string destinationNumber,
        DialplanDocument document,
        DialplanEntry entry,
        IReadOnlyList<DialplanAction> resolvedActions)
    {
        var condition = new XElement("condition",
            new XAttribute("field", entry.Match.Field ?? "destination_number"),
            new XAttribute("expression", BuildExpression(entry.Match, destinationNumber)));

        foreach (var a in resolvedActions)
            condition.Add(BuildAction(a));

        var doc = BuildDocument(
            new XElement("section",
                new XAttribute("name", "dialplan"),
                new XElement("context",
                    new XAttribute("name", context),
                    new XElement("extension",
                        new XAttribute("name", entry.Name ?? document.Name ?? "matched"),
                        condition))));

        return Serialize(doc);
    }

    public string RenderInvalidRoute(string context, string destinationNumber)
    {
        var doc = BuildDocument(
            new XElement("section",
                new XAttribute("name", "dialplan"),
                new XElement("context",
                    new XAttribute("name", string.IsNullOrEmpty(context) ? "default" : context),
                    new XElement("extension",
                        new XAttribute("name", "invalid-route"),
                        new XElement("condition",
                            new XAttribute("field", "destination_number"),
                            new XAttribute("expression", "^" + Regex.Escape(destinationNumber ?? string.Empty) + "$"),
                            new XElement("action",
                                new XAttribute("application", "answer")),
                            new XElement("action",
                                new XAttribute("application", "playback"),
                                new XAttribute("data", "ivr/ivr-that_was_an_invalid_entry.wav")),
                            new XElement("action",
                                new XAttribute("application", "hangup"),
                                new XAttribute("data", "NO_ROUTE_DESTINATION")))))));

        return Serialize(doc);
    }

    public string RenderNotFound()
    {
        var doc = BuildDocument(
            new XElement("section",
                new XAttribute("name", "result"),
                new XElement("result",
                    new XAttribute("status", "not found"))));
        return Serialize(doc);
    }

    private static XDocument BuildDocument(XElement section)
    {
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("document",
                new XAttribute("type", "freeswitch/xml"),
                section));
    }

    private static XElement BuildAction(DialplanAction a)
    {
        var el = new XElement("action", new XAttribute("application", a.Application));
        if (!string.IsNullOrEmpty(a.Data))
            el.Add(new XAttribute("data", a.Data));
        return el;
    }

    private static string BuildExpression(DialplanMatch match, string destinationNumber)
    {
        var type = (match.Type ?? "regex").ToLowerInvariant();
        return type switch
        {
            "exact" => "^" + System.Text.RegularExpressions.Regex.Escape(match.Value ?? destinationNumber ?? string.Empty) + "$",
            "prefix" => "^" + System.Text.RegularExpressions.Regex.Escape(match.Pattern ?? match.Value ?? string.Empty),
            _ => match.Pattern ?? "^" + System.Text.RegularExpressions.Regex.Escape(destinationNumber ?? string.Empty) + "$",
        };
    }

    private static string Serialize(XDocument doc)
    {
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false,
        });
        doc.Save(writer);
        writer.Flush();
        return sb.ToString();
    }

    private static class Regex
    {
        public static string Escape(string s) => System.Text.RegularExpressions.Regex.Escape(s);
    }
}
