using Microsoft.Extensions.Logging;
using VoiceFlow.Api.LiveChat.Infrastructure.Mongo;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Logging;

public sealed class AgentHubLogWriter
{
    private readonly LiveChatMongoContext _context;
    private readonly ILogger<AgentHubLogWriter> _logger;

    public AgentHubLogWriter(LiveChatMongoContext context, ILogger<AgentHubLogWriter> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> TryWriteAsync(
        string level,
        string category,
        string message,
        IDictionary<string, object?>? properties = null,
        Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = new AgentHubLogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message,
                Exception = exception?.ToString(),
                Properties = properties?.ToDictionary(kv => kv.Key, kv => Normalize(kv.Value)) ?? new Dictionary<string, string?>(),
            };

            await _context.AgentHubLogs.InsertOneAsync(entry, cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist AgentHub diagnostic log category={Category} message={Message}",
                category, message);
            return false;
        }
    }

    private static string? Normalize(object? value) => value switch
    {
        null => null,
        DateTime dateTime => dateTime.ToString("O"),
        bool boolValue => boolValue ? "true" : "false",
        Enum enumValue => enumValue.ToString(),
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
    };
}