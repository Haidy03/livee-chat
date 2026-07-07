using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoiceFlow.Api.LiveChat.Application;
using VoiceFlow.Api.LiveChat.Application.Abstractions;

namespace VoiceFlow.Api.LiveChat.Workers;

public sealed class OfferTimeoutWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<OfferTimeoutWorker> _log;
    public OfferTimeoutWorker(IServiceScopeFactory scopes, ILogger<OfferTimeoutWorker> log)
    { _scopes = scopes; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var timeouts = scope.ServiceProvider.GetRequiredService<IOfferTimeoutStore>();
                var routing = scope.ServiceProvider.GetRequiredService<RoutingEngine>();
                var expired = await timeouts.PopExpiredAsync();
                foreach (var (requestId, agentId) in expired)
                    await routing.ReleaseAndRequeueAsync(requestId, agentId, stoppingToken);
            }
            catch (Exception ex) { _log.LogWarning(ex, "OfferTimeoutWorker tick failed"); }

            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) { }
        }
    }
}
