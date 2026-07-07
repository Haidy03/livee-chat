using Contact_Center.Worker.Events.CallEvent.Messaging;

namespace Contact_Center.Worker.Events.CallEvent.Workers;


public sealed class CallTerminatedEventConsumer
    : BackgroundService
{
    private readonly CallMqConsumer _consumer;

    private readonly ILogger<CallTerminatedEventConsumer>
        _logger;

    public CallTerminatedEventConsumer(
        CallMqConsumer consumer,
        ILogger<CallTerminatedEventConsumer> logger)
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
