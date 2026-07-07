using System.Text;
using System.Text.Json;
using Contact_Center.Worker.Events.CallEvent.Handlers;
using Contact_Center.Worker.Events.CallEvent.Options;
using HelperLib.Messaging;
using Microsoft.Extensions.Options;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using VoiceFlow.Contracts.Events;

namespace Contact_Center.Worker.Events.CallEvent.Messaging;

public sealed class CallMqConsumer
{
    private readonly RabbitMqConnection _connectionFactory;

    private readonly CallConsumerOptions _options;

    private readonly ILogger<CallMqConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public CallMqConsumer(
        RabbitMqConnection connectionFactory,
        IOptions<CallConsumerOptions> callConsumerOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<CallMqConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _options = callConsumerOptions.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        var channel =
            await _connectionFactory.CreateChannelAsync(
                cancellationToken);

        await CallExchangeTopology.DeclareConsumerAsync(
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
                    JsonSerializer.Deserialize<CallTerminatedEvent>(
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


                using var scope = _scopeFactory.CreateScope();

                var handler =
                    scope.ServiceProvider
                        .GetRequiredService<CallTerminatedEventHandler>();

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
