using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using VoiceFlow.Api.LiveChat.Config;

namespace VoiceFlow.Api.LiveChat.Application.AiSuggestions;

public sealed class AiSuggestRateLimitException : Exception
{
    public AiSuggestRateLimitException(string message) : base(message) { }
}

/// <summary>
/// Simple in-memory sliding-window limiter. Per-pod only; sufficient for a
/// single API instance and matches the request in the AI Suggest spec.
/// </summary>
public sealed class AiSuggestRateLimiter
{
    private readonly IOptions<AiSuggestOptions> _options;
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _agentBuckets = new();
    private readonly ConcurrentDictionary<string, Queue<DateTime>> _tenantBuckets = new();

    public AiSuggestRateLimiter(IOptions<AiSuggestOptions> options) => _options = options;

    public void Check(string projectId, string agentId)
    {
        var opts = _options.Value;
        var now = DateTime.UtcNow;

        Enforce(_agentBuckets, $"{projectId}:{agentId}", TimeSpan.FromMinutes(1),
            Math.Max(1, opts.MaxSuggestionsPerMinutePerAgent),
            "Agent AI Suggest rate limit reached. Please slow down.");

        Enforce(_tenantBuckets, projectId, TimeSpan.FromHours(1),
            Math.Max(1, opts.MaxSuggestionsPerHourPerTenant),
            "Tenant AI Suggest hourly limit reached.");
    }

    private static void Enforce(ConcurrentDictionary<string, Queue<DateTime>> buckets, string key, TimeSpan window, int max, string message)
    {
        var q = buckets.GetOrAdd(key, _ => new Queue<DateTime>());
        lock (q)
        {
            var now = DateTime.UtcNow;
            var cutoff = now - window;
            while (q.Count > 0 && q.Peek() < cutoff) q.Dequeue();
            if (q.Count >= max) throw new AiSuggestRateLimitException(message);
            q.Enqueue(now);
        }
    }
}
