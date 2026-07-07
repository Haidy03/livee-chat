using Contact_Center.Worker.Events.Reports.Messaging;

namespace Contact_Center.Worker.Events.CallEvent.Workers;


public sealed class ReportProcessingEventConsumer
    : BackgroundService
{
    private readonly ReportMqConsumer _consumer;

    private readonly ILogger<ReportProcessingEventConsumer>
        _logger;

    public ReportProcessingEventConsumer(
        ReportMqConsumer consumer,
        ILogger<ReportProcessingEventConsumer> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        await _consumer.StartAsync(stoppingToken);

        _logger.LogInformation(
            "Background RabbitMQ worker started.");

        await Task.Delay(
            Timeout.Infinite,
            stoppingToken);
    }
}
