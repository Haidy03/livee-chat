namespace CtiBackend.Services.QueueMonitoring;

public sealed record AgentIdentity(string AgentId, string Interface, string? StateInterface, string? MemberName);

public interface IAgentIdentityNormalizer
{
    AgentIdentity Normalize(string? interfaceName, string? stateInterface, string? memberName);
}

public sealed class AgentIdentityNormalizer : IAgentIdentityNormalizer
{
    public AgentIdentity Normalize(string? interfaceName, string? stateInterface, string? memberName)
    {
        var iface = interfaceName ?? stateInterface ?? memberName ?? "unknown";
        var id = StripChannelSuffix(iface);
        return new AgentIdentity(id, iface, stateInterface, memberName);
    }

    /// <summary>
    /// Strips Asterisk channel-leg suffixes such as "-0000012a" so that
    /// "PJSIP/1001-0000012a" normalizes to "PJSIP/1001". Also normalizes
    /// "Local/1001@agents/n" → "Local/1001@agents".
    /// </summary>
    private static string StripChannelSuffix(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        // Strip "/n" or "/nz" Local channel suffix
        if (s.StartsWith("Local/", StringComparison.OrdinalIgnoreCase))
        {
            var slash = s.LastIndexOf('/');
            if (slash > "Local/".Length)
            {
                var tail = s[(slash + 1)..];
                if (tail.Length <= 2 && tail.All(c => c == 'n' || c == 'z'))
                    s = s[..slash];
            }
        }
        // Strip "-XXXXXXXX" hex channel-leg id
        var dash = s.LastIndexOf('-');
        if (dash > 0 && dash < s.Length - 1)
        {
            var tail = s[(dash + 1)..];
            if (tail.Length >= 4 && tail.Length <= 12 && tail.All(IsHex))
                return s[..dash];
        }
        return s;
    }

    private static bool IsHex(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}
