using System.Text.RegularExpressions;

namespace CtiBackend.Services.QueueMonitoring;

/// <summary>
/// Parses tenant-scoped Asterisk queue names that follow the conventions
/// <c>t_{tenantId}__q_{queueId}</c> (regular queues) and
/// <c>t_{tenantId}__qc_{campaignId}</c> (campaign queues). Used by the
/// queue-monitoring pipeline to derive the tenant per AMI event so Redis keys
/// are written under the correct tenant rather than the singleton
/// <see cref="AmiConnectionContext"/> fallback value.
/// </summary>
public static class QueueNameParser
{
    // Non-greedy tenant capture so the unique "__q_" / "__qc_" separator wins even if
    // the tenant itself contains underscores. The optional 'c' matches campaign queues.
    // Queue/campaign id allows anything to EOL.
    private static readonly Regex Pattern = new(
        @"^t_(?<t>.+?)__qc?_(?<q>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool TryParse(string? queueName, out string tenantId, out string queueId)
    {
        tenantId = string.Empty;
        queueId = string.Empty;
        if (string.IsNullOrEmpty(queueName)) return false;
        var m = Pattern.Match(queueName);
        if (!m.Success) return false;
        tenantId = m.Groups["t"].Value;
        queueId = m.Groups["q"].Value;
        return tenantId.Length > 0 && queueId.Length > 0;
    }
}
