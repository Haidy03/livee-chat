using System.ComponentModel.DataAnnotations;

namespace VoiceFlow.Infrastructure.Options;

/// <summary>Bound from the "MariaDB" configuration section (Asterisk realtime / dialplan metadata).</summary>
public sealed class MariaDbOptions
{
    public const string SectionName = "MariaDB";

    /// <summary>
    /// ADO.NET connection string. When <see cref="AsteriskDbName"/> is set and the builder has no database,
    /// <see cref="AsteriskDbName"/> is applied automatically.
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; init; } = string.Empty;

    public string AsteriskDbName { get; init; } = "asterisk";

    public string EndpointsTable { get; init; } = "ps_endpoints";

    public string AuthsTable { get; init; } = "ps_auths";

    public string AorsTable { get; init; } = "ps_aors";

    public string ContactsTable { get; init; } = "ps_contacts";

    public string IdentifyTable { get; init; } = "ps_endpoint_id_ips";

    public string ExtensionsTable { get; init; } = "extensions";
}
