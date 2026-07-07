using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace VoiceFlow.Api.LiveChat.Infrastructure.Logging;

public sealed record LiveChatRedisDiagnostics(
    string EnvironmentName,
    bool HasConfiguredRedisConnectionString,
    bool UsedDevelopmentFallback,
    IReadOnlyCollection<string> Endpoints,
    bool UsesLocalhostEndpoint,
    string? ConfigurationError,
    bool SignalRRedisBackplaneRequested,
    bool SignalRRedisBackplaneRegistered,
    string SignalRRedisBackplaneReason,
    ConfigurationOptions? ConfigurationOptions,
    string SourceKey,
    bool RedisSettingsEnabled)
{
    public Dictionary<string, object?> ToLogProperties(IDictionary<string, object?>? additional = null)
    {
        var properties = new Dictionary<string, object?>
        {
            ["environment"] = EnvironmentName,
            ["sourceKey"] = SourceKey,
            ["redisSettingsEnabled"] = RedisSettingsEnabled,
            ["hasRedisConnectionString"] = HasConfiguredRedisConnectionString,
            ["usedDevelopmentFallback"] = UsedDevelopmentFallback,
            ["redisEndpointCount"] = Endpoints.Count,
            ["redisEndpoints"] = string.Join(",", Endpoints),
            ["usesLocalhostEndpoint"] = UsesLocalhostEndpoint,
            ["configurationError"] = ConfigurationError,
            ["signalRRedisBackplaneRequested"] = SignalRRedisBackplaneRequested,
            ["signalRRedisBackplaneRegistered"] = SignalRRedisBackplaneRegistered,
            ["signalRRedisBackplaneReason"] = SignalRRedisBackplaneReason,
        };


        if (additional is not null)
        {
            foreach (var (key, value) in additional)
                properties[key] = value;
        }

        return properties;
    }
}

public sealed class LiveChatRedisDiagnosticsHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly LiveChatRedisDiagnostics _diagnostics;

    public LiveChatRedisDiagnosticsHostedService(
        IServiceProvider services,
        LiveChatRedisDiagnostics diagnostics)
    {
        _services = services;
        _diagnostics = diagnostics;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var writer = scope.ServiceProvider.GetRequiredService<AgentHubLogWriter>();
        var storeInfo = scope.ServiceProvider.GetService<VoiceFlow.Api.LiveChat.Config.LiveChatStoreInfo>();
        var storeProps = new Dictionary<string, object?>
        {
            ["presenceStore"] = storeInfo?.PresenceStoreType,
            ["offerTimeoutStore"] = storeInfo?.OfferTimeoutStoreType,
        };

        await writer.TryWriteAsync(
            _diagnostics.ConfigurationError is null ? "Information" : "Error",
            "LiveChatRedisStartupDiagnostics",
            $"LiveChat Redis startup diagnostics initialized (presenceStore={storeInfo?.PresenceStoreType ?? "unknown"})",
            _diagnostics.ToLogProperties(storeProps),
            cancellationToken: cancellationToken);


        try
        {
            var mux = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
            await writer.TryWriteAsync(
                mux.IsConnected ? "Information" : "Error",
                "LiveChatRedisStartupDiagnostics",
                mux.IsConnected ? "LiveChat Redis is connected" : "LiveChat Redis is not connected",
                _diagnostics.ToLogProperties(new Dictionary<string, object?>
                {
                    ["isConnected"] = mux.IsConnected,
                    ["clientName"] = mux.ClientName,
                }),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await writer.TryWriteAsync(
                "Error",
                "LiveChatRedisStartupDiagnostics",
                "LiveChat Redis diagnostics could not resolve IConnectionMultiplexer",
                _diagnostics.ToLogProperties(new Dictionary<string, object?>
                {
                    ["exceptionType"] = ex.GetType().FullName,
                    ["exceptionMessage"] = ex.Message,
                }),
                ex,
                cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}