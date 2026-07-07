using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using VoiceFlow.FreeSwitchXmlCurl.Models;
using VoiceFlow.FreeSwitchXmlCurl.Settings;

namespace VoiceFlow.FreeSwitchXmlCurl.Services;

public sealed class DialplanService : IDialplanService
{
    private readonly IMongoCollection<DialplanDocument> _collection;
    private readonly ITemplateResolver _templates;
    private readonly IXmlDialplanRenderer _renderer;
    private readonly FreeSwitchSettings _fs;
    private readonly ILogger<DialplanService> _log;

    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    public DialplanService(
        IMongoCollection<DialplanDocument> collection,
        ITemplateResolver templates,
        IXmlDialplanRenderer renderer,
        IOptions<FreeSwitchSettings> fs,
        ILogger<DialplanService> log)
    {
        _collection = collection;
        _templates = templates;
        _renderer = renderer;
        _fs = fs.Value;
        _log = log;
    }

    public async Task<string> HandleAsync(IReadOnlyDictionary<string, string> p, CancellationToken ct = default)
    {
        var section = Get(p, "section");
        if (!string.Equals(section, "dialplan", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogInformation("Unsupported section '{Section}' — returning not-found XML", section);
            return _renderer.RenderNotFound();
        }

        var context = Get(p, "context");
        var destination = Get(p, "destination_number");
        var caller = Get(p, "caller_id_number");

        var domain = Get(p, "domain_name");
        if (string.IsNullOrEmpty(domain)) domain = Get(p, "domain");
        if (string.IsNullOrEmpty(domain) && string.Equals(context, "from-pstn", StringComparison.OrdinalIgnoreCase))
            domain = _fs.DefaultInboundDomain;

        _log.LogInformation(
            "FS xml_curl request domain={Domain} context={Context} destination={Destination} caller={Caller}",
            domain, context, destination, caller);

        if (string.IsNullOrEmpty(context) || string.IsNullOrEmpty(domain))
        {
            _log.LogWarning("Missing domain or context; returning invalid-route XML");
            return _renderer.RenderInvalidRoute(context, destination);
        }

        var filter = Builders<DialplanDocument>.Filter.And(
            Builders<DialplanDocument>.Filter.Eq(x => x.Domain, domain),
            Builders<DialplanDocument>.Filter.Eq(x => x.Context, context),
            Builders<DialplanDocument>.Filter.Eq(x => x.Enabled, true));

        var docs = await _collection.Find(filter)
            .SortBy(x => x.Priority)
            .ToListAsync(ct);

        foreach (var doc in docs)
        {
            foreach (var entry in doc.Entries.OrderBy(e => e.Priority))
            {
                var field = entry.Match?.Field ?? "destination_number";
                var fieldValue = Get(p, field);
                if (!TryMatch(entry.Match!, fieldValue)) continue;

                _log.LogInformation(
                    "Matched dialplan_id={DialplanId} entry={Entry} routeType={RouteType}",
                    doc.Id, entry.Name, entry.RouteType);

                var variables = BuildVariables(p, domain, context, destination, doc, entry);
                var resolved = entry.Actions.Select(a => new DialplanAction
                {
                    Application = a.Application,
                    Data = string.IsNullOrEmpty(a.Data) ? a.Data : _templates.Resolve(a.Data, variables),
                }).ToList();

                return _renderer.RenderMatched(context, destination, doc, entry, resolved);
            }
        }

        _log.LogInformation("No matching dialplan entry for {Destination} in {Context}/{Domain}",
            destination, context, domain);
        return _renderer.RenderInvalidRoute(context, destination);
    }

    private static bool TryMatch(DialplanMatch m, string value)
    {
        var type = (m?.Type ?? "regex").ToLowerInvariant();
        switch (type)
        {
            case "exact":
                return !string.IsNullOrEmpty(m!.Value) && string.Equals(value, m.Value, StringComparison.Ordinal);
            case "prefix":
                {
                    var prefix = m!.Pattern ?? m.Value;
                    return !string.IsNullOrEmpty(prefix) && value != null && value.StartsWith(prefix, StringComparison.Ordinal);
                }
            case "regex":
            default:
                {
                    if (string.IsNullOrEmpty(m!.Pattern)) return false;
                    var rx = RegexCache.GetOrAdd(m.Pattern, p => new Regex(p, RegexOptions.Compiled));
                    return rx.IsMatch(value ?? string.Empty);
                }
        }
    }

    private static Dictionary<string, string> BuildVariables(
        IReadOnlyDictionary<string, string> p,
        string domain,
        string context,
        string destination,
        DialplanDocument doc,
        DialplanEntry entry)
    {
        var v = new Dictionary<string, string>(p, StringComparer.Ordinal);
        v["domain_name"] = domain;
        v["domain"] = domain;
        v["context"] = context;
        v["destination_number"] = destination ?? string.Empty;
        v["normalized_destination_number"] = Normalize(destination);
        v["tenant_id"] = Get(p, "tenant_id");
        if (string.IsNullOrEmpty(v["tenant_id"]) && !string.IsNullOrEmpty(doc.TenantId))
            v["tenant_id"] = doc.TenantId!;
        v["dialplan_id"] = doc.Id ?? string.Empty;
        v["dialplan_name"] = doc.Name ?? string.Empty;
        v["entry_name"] = entry.Name ?? string.Empty;
        v["route_type"] = entry.RouteType ?? string.Empty;
        return v;
    }

    private static string Normalize(string? destination)
    {
        if (string.IsNullOrEmpty(destination)) return string.Empty;
        return destination.StartsWith("+") ? destination[1..] : destination;
    }

    private static string Get(IReadOnlyDictionary<string, string> p, string key)
        => p.TryGetValue(key, out var v) ? v ?? string.Empty : string.Empty;
}
