using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using VoiceFlow.Api.LiveChat.Application;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Application.AiSuggestions;
using VoiceFlow.Api.LiveChat.Hubs;
using VoiceFlow.Api.LiveChat.Infrastructure.Ai;
using VoiceFlow.Api.LiveChat.Infrastructure.Channels;
using VoiceFlow.Api.LiveChat.Infrastructure.Logging;
using VoiceFlow.Api.LiveChat.Infrastructure.Mongo;
using VoiceFlow.Api.LiveChat.Infrastructure.Redis;
using VoiceFlow.Api.LiveChat.Workers;

namespace VoiceFlow.Api.LiveChat.Config;

public static class ServiceRegistration
{
    public static IServiceCollection AddLiveChat(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LiveChatOptions>(configuration.GetSection(LiveChatOptions.SectionName));
        var liveChatOptions = configuration.GetSection(LiveChatOptions.SectionName).Get<LiveChatOptions>() ?? new LiveChatOptions();
        var redisDiagnostics = BuildRedisDiagnostics(configuration, liveChatOptions);

        // Mongo — reuse existing IMongoClient and MongoDbSettings from VoiceFlow.Infrastructure.
        services.AddSingleton<LiveChatMongoContext>();
        services.AddSingleton<LiveChatCollectionBootstrap>();
        services.AddSingleton<AgentHubLogWriter>();
        services.AddSingleton(redisDiagnostics);
        services.AddHostedService<LiveChatRedisDiagnosticsHostedService>();
        services.AddScoped<IClientRequestRepository, ClientRequestRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<ICannedResponseRepository, CannedResponseRepository>();
        services.AddScoped<IAiSuggestionRepository, AiSuggestionRepository>();

        // AI Suggest
        services.Configure<AiSuggestOptions>(configuration.GetSection(AiSuggestOptions.SectionName));
        var aiOpts = configuration.GetSection(AiSuggestOptions.SectionName).Get<AiSuggestOptions>() ?? new AiSuggestOptions();
        services.AddHttpClient("aisuggest");
        if (string.Equals(aiOpts.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<ILlmClient, OpenAiLlmClient>();
        else
            services.AddSingleton<ILlmClient, MockLlmClient>();
        services.AddSingleton<AiSuggestRateLimiter>();
        services.AddScoped<IAiSuggestionContextBuilder, AiSuggestionContextBuilder>();
        services.AddSingleton<IAiSuggestionPromptBuilder, AiSuggestionPromptBuilder>();
        services.AddScoped<IAiSuggestionService, AiSuggestionService>();

        // Redis — application presence/offer-timeout storage is separate from the optional SignalR backplane.
        // When Redis is unavailable/misconfigured we fall back to a pod-local in-memory implementation
        // so the AgentHub can still accept connections. This is a single-pod stopgap.
        var useInMemoryFallback = redisDiagnostics.ConfigurationError is not null
            || redisDiagnostics.ConfigurationOptions is null;

        string presenceStoreType;
        string offerTimeoutStoreType;
        if (!useInMemoryFallback)
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var writer = sp.GetRequiredService<AgentHubLogWriter>();
                var mux = ConnectionMultiplexer.Connect(redisDiagnostics.ConfigurationOptions!);
                AttachRedisConnectionDiagnostics(mux, writer, redisDiagnostics);
                return mux;
            });
            services.AddSingleton<IPresenceStore, PresenceStore>();
            services.AddSingleton<IOfferTimeoutStore, OfferTimeoutStore>();
            presenceStoreType = nameof(PresenceStore);
            offerTimeoutStoreType = nameof(OfferTimeoutStore);
        }
        else
        {
            services.AddSingleton<IPresenceStore, Infrastructure.Presence.InMemoryPresenceStore>();
            services.AddSingleton<IOfferTimeoutStore, Infrastructure.Presence.InMemoryOfferTimeoutStore>();
            presenceStoreType = nameof(Infrastructure.Presence.InMemoryPresenceStore);
            offerTimeoutStoreType = nameof(Infrastructure.Presence.InMemoryOfferTimeoutStore);
        }
        services.AddSingleton(new LiveChatStoreInfo(presenceStoreType, offerTimeoutStoreType));




        // Application services
        services.AddScoped<RoutingEngine>();
        services.AddScoped<RoomService>();
        services.AddScoped<ClientRequestService>();
        services.AddScoped<ChannelDispatcher>();

        // Channel adapters
        services.AddScoped<IChannelAdapter, WebWidgetChannelAdapter>();
        services.AddScoped<IChannelAdapter, MobileAppChannelAdapter>();
        services.AddScoped<IChannelAdapter, WhatsAppChannelAdapter>();
        services.AddScoped<IChannelAdapter, MessengerChannelAdapter>();

        services.AddHttpClient("whatsapp", (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LiveChatOptions>>().Value;
            if (!string.IsNullOrEmpty(opts.Channels.WhatsApp.BaseUrl))
                client.BaseAddress = new Uri(opts.Channels.WhatsApp.BaseUrl);
            if (!string.IsNullOrEmpty(opts.Channels.WhatsApp.Token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.Channels.WhatsApp.Token);
        });
        services.AddHttpClient("messenger", (sp, client) =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LiveChatOptions>>().Value;
            if (!string.IsNullOrEmpty(opts.Channels.Messenger.BaseUrl))
                client.BaseAddress = new Uri(opts.Channels.Messenger.BaseUrl);
            if (!string.IsNullOrEmpty(opts.Channels.Messenger.Token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", opts.Channels.Messenger.Token);
        });

        // Hosted services
        services.AddHostedService<OfferTimeoutWorker>();
        services.AddHostedService<StaleRequestSweeper>();

        // AgentHub diagnostics are written from middleware before SignalR activates the hub.
        services.AddSingleton<AgentHubDiagnosticsFilter>();

        // Route SignalR framework logs to the AgentHubLog Mongo collection so we can see
        // handshake timeouts / protocol errors that happen before hub activation.
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider, AgentHubSignalRLoggerProvider>();

        var signalRBuilder = services
            .AddSignalR(options =>
            {
                options.AddFilter<AgentHubDiagnosticsFilter>();
                options.HandshakeTimeout = TimeSpan.FromSeconds(30);
                options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.EnableDetailedErrors = true;
            });

        if (redisDiagnostics.SignalRRedisBackplaneRegistered && redisDiagnostics.ConfigurationOptions is not null)
        {
            signalRBuilder.AddStackExchangeRedis(redisDiagnostics.ConfigurationOptions.ToString(), _ => { });
        }

        return services;
    }

    public static IEndpointRouteBuilder MapLiveChat(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<AgentHub>("/hubs/agent");
        endpoints.MapHub<CustomerHub>("/hubs/customer");
        endpoints.MapLiveChatDiagnostics();
        return endpoints;
    }


    private static LiveChatRedisDiagnostics BuildRedisDiagnostics(IConfiguration configuration, LiveChatOptions liveChatOptions)
    {
        var environmentName = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? configuration["DOTNET_ENVIRONMENT"]
            ?? "Production";
        var isProduction = string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase);

        var redisSection = configuration.GetSection("RedisSettings");
        var redisSettingsEnabled = redisSection.GetValue<bool?>("Enabled") ?? true;
        var sentinelConnectionString = redisSection["SentinelConnectionString"];
        var connectionString = redisSection["ConnectionString"];

        string? effectiveRedisConnectionString;
        string sourceKey;
        if (!string.IsNullOrWhiteSpace(sentinelConnectionString))
        {
            effectiveRedisConnectionString = sentinelConnectionString;
            sourceKey = "RedisSettings:SentinelConnectionString";
        }
        else if (!string.IsNullOrWhiteSpace(connectionString))
        {
            effectiveRedisConnectionString = connectionString;
            sourceKey = "RedisSettings:ConnectionString";
        }
        else
        {
            effectiveRedisConnectionString = null;
            sourceKey = "RedisSettings";
        }
        var hasConfiguredRedisConnectionString = !string.IsNullOrWhiteSpace(effectiveRedisConnectionString);

        ConfigurationOptions? redisOptions = null;
        string? configurationError = null;
        if (!redisSettingsEnabled)
        {
            configurationError = "RedisSettings:Enabled is false.";
        }
        else if (!hasConfiguredRedisConnectionString)
        {
            configurationError = "RedisSettings:ConnectionString / SentinelConnectionString are missing.";
        }
        else
        {
            try
            {
                redisOptions = ConfigurationOptions.Parse(effectiveRedisConnectionString!);
                redisOptions.AbortOnConnectFail = false;
                redisOptions.ConnectRetry = 3;
                redisOptions.ConnectTimeout = Math.Max(redisOptions.ConnectTimeout, 5000);
            }
            catch (Exception ex)
            {
                configurationError = $"{sourceKey} is invalid: {ex.GetType().Name}";
            }
        }

        var endpoints = redisOptions?.EndPoints
            .Select(e => e.ToString())
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => s!)
            .ToArray() ?? Array.Empty<string>();
        var usesLocalhostEndpoint = endpoints.Any(IsLocalhostEndpoint);
        if (configurationError is null && isProduction && usesLocalhostEndpoint)
        {
            configurationError = $"{sourceKey} points to localhost in Production.";
        }

        // Probe connectivity synchronously with a short timeout so a bad/unreachable
        // endpoint triggers the in-memory fallback at startup instead of stalling
        // every hub call for 5s.
        if (configurationError is null && redisOptions is not null)
        {
            try
            {
                var probeOptions = redisOptions.Clone();
                probeOptions.AbortOnConnectFail = true;
                probeOptions.ConnectTimeout = 2000;
                probeOptions.ConnectRetry = 1;
                probeOptions.SyncTimeout = 2000;
                using var probe = ConnectionMultiplexer.Connect(probeOptions);
                if (!probe.IsConnected)
                    configurationError = $"{sourceKey} endpoint is unreachable.";
            }
            catch (Exception ex)
            {
                configurationError = $"{sourceKey} probe failed: {ex.GetType().Name}: {ex.Message}";
            }
        }

        var signalRRedisBackplaneRequested = liveChatOptions.SignalR.UseRedisBackplane;
        var signalRRedisBackplaneRegistered = signalRRedisBackplaneRequested
            && configurationError is null
            && redisOptions is not null
            && (!isProduction || !usesLocalhostEndpoint);

        var signalRRedisBackplaneReason = signalRRedisBackplaneRegistered
            ? "registered"
            : !signalRRedisBackplaneRequested
                ? "disabled_by_configuration"
                : !redisSettingsEnabled
                    ? "disabled_by_RedisSettings"
                    : redisOptions is null
                        ? configurationError ?? "redis_configuration_unavailable"
                        : isProduction && usesLocalhostEndpoint
                            ? "localhost_endpoint_rejected_in_production"
                            : "not_registered";

        return new LiveChatRedisDiagnostics(
            environmentName,
            hasConfiguredRedisConnectionString,
            UsedDevelopmentFallback: false,
            endpoints,
            usesLocalhostEndpoint,
            configurationError,
            signalRRedisBackplaneRequested,
            signalRRedisBackplaneRegistered,
            signalRRedisBackplaneReason,
            configurationError is null ? redisOptions : null,
            sourceKey,
            redisSettingsEnabled);
    }


    private static bool IsLocalhostEndpoint(string endpoint)
    {
        // StackExchange.Redis renders DnsEndPoint as "Unspecified/localhost:6379"
        // and IPEndPoint as "InterNetwork/127.0.0.1:6379". Strip the AddressFamily
        // prefix before extracting the host, otherwise "localhost" is never seen.
        var normalized = endpoint;
        var slash = normalized.IndexOf('/');
        if (slash >= 0 && slash < normalized.Length - 1)
            normalized = normalized[(slash + 1)..];

        string host;
        if (normalized.StartsWith('['))
        {
            var close = normalized.IndexOf(']');
            host = close > 0 ? normalized.Substring(1, close - 1) : normalized.Trim('[', ']');
        }
        else
        {
            var colon = normalized.LastIndexOf(':');
            host = colon > 0 ? normalized[..colon] : normalized;
        }

        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    private static void AttachRedisConnectionDiagnostics(
        IConnectionMultiplexer mux,
        AgentHubLogWriter writer,
        LiveChatRedisDiagnostics diagnostics)
    {
        _ = writer.TryWriteAsync(
            mux.IsConnected ? "Information" : "Error",
            "LiveChatRedis",
            mux.IsConnected ? "LiveChat Redis multiplexer connected" : "LiveChat Redis multiplexer is not connected",
            diagnostics.ToLogProperties(new Dictionary<string, object?>
            {
                ["isConnected"] = mux.IsConnected,
                ["clientName"] = mux.ClientName,
            }));

        mux.ConnectionFailed += (_, args) =>
        {
            _ = writer.TryWriteAsync(
                "Error",
                "LiveChatRedis",
                "LiveChat Redis connection failed",
                diagnostics.ToLogProperties(new Dictionary<string, object?>
                {
                    ["endpoint"] = args.EndPoint?.ToString(),
                    ["connectionType"] = args.ConnectionType.ToString(),
                    ["failureType"] = args.FailureType.ToString(),
                }),
                args.Exception);
        };

        mux.ConnectionRestored += (_, args) =>
        {
            _ = writer.TryWriteAsync(
                "Information",
                "LiveChatRedis",
                "LiveChat Redis connection restored",
                diagnostics.ToLogProperties(new Dictionary<string, object?>
                {
                    ["endpoint"] = args.EndPoint?.ToString(),
                    ["connectionType"] = args.ConnectionType.ToString(),
                    ["failureType"] = args.FailureType.ToString(),
                }),
                args.Exception);
        };

        mux.ErrorMessage += (_, args) =>
        {
            _ = writer.TryWriteAsync(
                "Warning",
                "LiveChatRedis",
                "LiveChat Redis error message",
                diagnostics.ToLogProperties(new Dictionary<string, object?>
                {
                    ["endpoint"] = args.EndPoint?.ToString(),
                    ["message"] = args.Message,
                }));
        };
    }
}
