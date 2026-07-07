using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Logging;

/// <summary>
/// ILoggerProvider that mirrors framework log messages from
/// `Microsoft.AspNetCore.SignalR.*` and `Microsoft.AspNetCore.Http.Connections.*`
/// into the AgentHubLog Mongo collection. This lets us see SignalR-side handshake
/// timeouts, protocol mismatches and hub activation failures without shipping raw
/// framework logs.
/// </summary>
public sealed class AgentHubSignalRLoggerProvider : ILoggerProvider
{
    private readonly IServiceProvider _services;

    public AgentHubSignalRLoggerProvider(IServiceProvider services)
    {
        _services = services;
    }

    public ILogger CreateLogger(string categoryName)
    {
        if (!ShouldCapture(categoryName)) return NullLogger.Instance;
        return new ForwardingLogger(categoryName, _services);
    }

    public void Dispose() { }

    private static bool ShouldCapture(string category) =>
        category.StartsWith("Microsoft.AspNetCore.SignalR", StringComparison.Ordinal)
        || category.StartsWith("Microsoft.AspNetCore.Http.Connections", StringComparison.Ordinal);

    private sealed class ForwardingLogger : ILogger
    {
        private readonly string _category;
        private readonly IServiceProvider _services;
        private static readonly Regex AccessTokenRegex = new("([?&]access_token=)[^&\\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public ForwardingLogger(string category, IServiceProvider services)
        {
            _category = category;
            _services = services;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = AccessTokenRegex.Replace(formatter(state, exception), "$1[redacted]");
            // Fire-and-forget — we never want framework logging to await Mongo.
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var writer = scope.ServiceProvider.GetRequiredService<AgentHubLogWriter>();
                    await writer.TryWriteAsync(
                        logLevel.ToString(),
                        _category,
                        message,
                        new Dictionary<string, object?>
                        {
                            ["eventId"] = eventId.Id,
                            ["eventName"] = eventId.Name,
                        },
                        exception);
                }
                catch
                {
                    /* never let logging bring down the process */
                }
            });
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }

    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
