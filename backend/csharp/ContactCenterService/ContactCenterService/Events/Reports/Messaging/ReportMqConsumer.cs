using System.Text;
using System.Text.Json;
using Contact_Center.Worker.Events.CallEvent.Options;
using Contact_Center.Worker.Events.Reports.Handlers;
using HelperLib.Messaging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VoiceFlow.Contracts.Events;

namespace Contact_Center.Worker.Events.Reports.Messaging;

public sealed class ReportMqConsumer
{
    private readonly RabbitMqConnection _connectionFactory;

    private readonly ReportConsumerOptions _options;

    private readonly ILogger<ReportMqConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public ReportMqConsumer(
        RabbitMqConnection connectionFactory,
        IOptions<ReportConsumerOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ReportMqConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        var channel =
            await _connectionFactory.CreateChannelAsync(
                cancellationToken);

        await ReportExchangeTopology.DeclareConsumerAsync(
            channel,
            _options,
            cancellationToken);

        var consumer =
            new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(
                    args.Body.ToArray());

                _logger.LogInformation(
                    "Received message: {Message}",
                    json);

                var message =
                    JsonSerializer.Deserialize<ReportRunRequested>(
                        json);

                if (message is null)
                {
                    await channel.BasicNackAsync(
                        args.DeliveryTag,
                        false,
                        false,
                        cancellationToken);

                    return;
                }

                _logger.LogInformation(
                    $"start processing report message at {DateTime.Now}");


                using var scope = _scopeFactory.CreateScope();

                var handler =
                    scope.ServiceProvider
                        .GetRequiredService<ReportHandler>();

                await handler.HandleAsync(
                    message,
                    cancellationToken);



                await channel.BasicAckAsync(
                    args.DeliveryTag,
                    false,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error processing RabbitMQ message.");

                if (channel.IsOpen)
                {
                    await channel.BasicNackAsync(
                        args.DeliveryTag,
                        false,
                        false,
                        cancellationToken);
                }
            }
        };

        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "RabbitMQ consumer registered for queue {QueueName}.",
            _options.QueueName);
    }
}
