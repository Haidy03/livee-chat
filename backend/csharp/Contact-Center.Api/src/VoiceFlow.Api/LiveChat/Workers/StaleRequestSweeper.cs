using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceFlow.Api.LiveChat.Application.Abstractions;
using VoiceFlow.Api.LiveChat.Config;

namespace VoiceFlow.Api.LiveChat.Workers;

public sealed class StaleRequestSweeper : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<StaleRequestSweeper> _log;
    private readonly LiveChatOptions _options;

    public StaleRequestSweeper(IServiceScopeFactory scopes, IOptions<LiveChatOptions> options, ILogger<StaleRequestSweeper> log)
    { _scopes = scopes; _options = options.Value; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var requests = scope.ServiceProvider.GetRequiredService<IClientRequestRepository>();
                var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(_options.StaleRequestGraceMinutes);
                var stale = await requests.GetStaleOfflineAsync(cutoff, stoppingToken);
                foreach (var r in stale)
                    await requests.DeleteAsync(r._id, ct: stoppingToken);
            }
            catch (Exception ex) { _log.LogWarning(ex, "StaleRequestSweeper tick failed"); }

            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) { }
        }
    }
}
