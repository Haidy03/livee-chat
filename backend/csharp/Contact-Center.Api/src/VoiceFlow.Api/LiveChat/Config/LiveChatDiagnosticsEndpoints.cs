using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Infrastructure.Logging;

namespace VoiceFlow.Api.LiveChat.Config;

internal static class LiveChatDiagnosticsEndpoints
{
    public static IEndpointRouteBuilder MapLiveChatDiagnostics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/livechat/diag/redis", async (
            HttpContext ctx,
            LiveChatStoreInfo storeInfo,
            LiveChatRedisDiagnostics diagnostics,
            IPresenceStore presence,
            IOfferTimeoutStore offers) =>
        {
            var redisInfo = new Dictionary<string, object?>
            {
                ["sourceKey"] = diagnostics.SourceKey,
                ["redisSettingsEnabled"] = diagnostics.RedisSettingsEnabled,
                ["endpoints"] = diagnostics.Endpoints,
                ["usesLocalhostEndpoint"] = diagnostics.UsesLocalhostEndpoint,
                ["configurationError"] = diagnostics.ConfigurationError,
                ["signalRBackplaneRegistered"] = diagnostics.SignalRRedisBackplaneRegistered,
                ["signalRBackplaneReason"] = diagnostics.SignalRRedisBackplaneReason,
                ["defaultDatabase"] = diagnostics.ConfigurationOptions?.DefaultDatabase,
            };


            var sampleKeys = new List<string>();
            var presenceKeys = new List<string>();
            bool? isConnected = null;

            var mux = ctx.RequestServices.GetService<IConnectionMultiplexer>();
            if (mux is not null)
            {
                isConnected = mux.IsConnected;
                try
                {
                    var db = mux.GetDatabase();
                    var endpoint = mux.GetEndPoints().FirstOrDefault();
                    if (endpoint is not null)
                    {
                        var server = mux.GetServer(endpoint);
                        await foreach (var k in server.KeysAsync(pattern: "livechat:*", pageSize: 50))
                        {
                            sampleKeys.Add(k.ToString());
                            if (sampleKeys.Count >= 50) break;
                        }
                        await foreach (var k in server.KeysAsync(pattern: "livechat:presence:*", pageSize: 50))
                        {
                            presenceKeys.Add(k.ToString());
                            if (presenceKeys.Count >= 50) break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    redisInfo["scanError"] = $"{ex.GetType().Name}: {ex.Message}";
                }
            }
            redisInfo["isConnected"] = isConnected;

            return Results.Json(new
            {
                presenceStore = storeInfo.PresenceStoreType,
                offerTimeoutStore = storeInfo.OfferTimeoutStoreType,
                presenceRuntimeType = presence.GetType().FullName,
                offerRuntimeType = offers.GetType().FullName,
                redis = redisInfo,
                sampleKeys,
                agentPresenceKeys = presenceKeys,
            });
        }).RequireAuthorization();

        return endpoints;
    }
}
