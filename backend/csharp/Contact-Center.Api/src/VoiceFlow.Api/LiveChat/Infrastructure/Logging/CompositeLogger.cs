using Microsoft.Extensions.Logging;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Logging;

/// <summary>
/// Fans one log call out to multiple ILogger sinks (e.g. default pipeline + Mongo).
/// </summary>
public sealed class CompositeLogger<T> : ILogger<T>
{
    private readonly ILogger[] _sinks;

    public CompositeLogger(params ILogger[] sinks) => _sinks = sinks;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => _sinks.Any(s => s.IsEnabled(logLevel));

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        foreach (var sink in _sinks)
        {
            try { sink.Log(logLevel, eventId, state, exception, formatter); }
            catch { /* one sink failure must not break the others */ }
        }
    }
}
