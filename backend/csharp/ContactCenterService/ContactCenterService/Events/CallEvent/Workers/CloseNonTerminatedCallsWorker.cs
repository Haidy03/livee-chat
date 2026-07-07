using VoiceFlow.Core.Interfaces.Repositories;

namespace Contact_Center.Worker.Events.CallEvent.Workers;

public sealed class CloseNonTerminatedCallsWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CloseNonTerminatedCallsWorker> _logger;
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(1);

    public CloseNonTerminatedCallsWorker(
        IServiceProvider serviceProvider,
        ILogger<CloseNonTerminatedCallsWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CloseNonTerminatedCallsWorker started. Running every {Hours} hours.", RunInterval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var callRepository = scope.ServiceProvider.GetRequiredService<ICallRepository>();
                    _logger.LogInformation("Starting periodic clean up of non-terminated calls.");
                    int closedCount = await callRepository.CloseNonTerminatedCallsAsync();
                    _logger.LogInformation("Successfully completed/closed {Count} non-terminated calls.", closedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while closing non-terminated calls.");
            }


            await Task.Delay(RunInterval, stoppingToken);

        }
    }
}
