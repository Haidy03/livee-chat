namespace CtiBackend.Services.HubSpot;

public sealed class HubSpotStateCleanupHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<HubSpotStateCleanupHostedService> _log;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public HubSpotStateCleanupHostedService(IServiceProvider sp, ILogger<HubSpotStateCleanupHostedService> log)
    { _sp = sp; _log = log; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IHubSpotIntegrationRepository>();
                var deleted = await repo.DeleteExpiredOrConsumedStatesAsync(
                    DateTime.UtcNow.AddHours(-1), stoppingToken);
                if (deleted > 0)
                    _log.LogInformation("HubSpot state cleanup deleted {Count} records", deleted);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "HubSpot state cleanup failed");
            }
            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
