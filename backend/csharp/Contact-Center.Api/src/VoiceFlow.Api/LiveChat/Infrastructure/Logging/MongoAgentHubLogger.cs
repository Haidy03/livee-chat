using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Logging;

/// <summary>
/// ILogger implementation that persists entries into the AgentHubLog Mongo collection.
/// Writes are fire-and-forget so a Mongo outage never breaks the hub. Fallback goes to
/// the supplied inner logger.
/// </summary>
public sealed class MongoAgentHubLogger : ILogger
{
    private readonly string _category;
    private readonly IServiceProvider _services;
    private readonly ILogger _fallback;

    public MongoAgentHubLogger(string category, IServiceProvider services, ILogger fallback)
    {
        _category = category;
        _services = services;
        _fallback = fallback;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var entry = new AgentHubLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = logLevel.ToString(),
            Category = _category,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = formatter(state, exception),
            Exception = exception?.ToString(),
        };

        if (state is IReadOnlyList<KeyValuePair<string, object?>> kvps)
        {
            foreach (var kv in kvps)
            {
                if (kv.Key == "{OriginalFormat}")
                    entry.Template = kv.Value?.ToString();
                else
                    entry.Properties[kv.Key] = kv.Value?.ToString();
            }
        }

        // Fire-and-forget insert. Never block the hub thread on Mongo.
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<LiveChatMongoContext>();
                await ctx.AgentHubLogs.InsertOneAsync(entry);
            }
            catch (Exception ex)
            {
                _fallback.Log(LogLevel.Warning, ex,
                    "MongoAgentHubLogger insert failed: {Message}", entry.Message);
            }
        });
    }
}
