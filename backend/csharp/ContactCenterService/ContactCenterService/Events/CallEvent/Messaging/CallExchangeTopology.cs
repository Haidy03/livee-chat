using Contact_Center.Worker.Events.CallEvent.Options;
using RabbitMQ.Client;

namespace Contact_Center.Worker.Events.CallEvent.Messaging;

internal static class CallExchangeTopology
{
    /// <summary>
    /// Declares the exchange, queue, binding, and QoS for this consumer.
    /// </summary>
    public static async Task DeclareConsumerAsync(
        IChannel channel,
        CallConsumerOptions options,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: options.ExchangeName,
            type: options.ExchangeType,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        var bindRoutingKey = ResolveBindRoutingKey(options);

        await channel.QueueBindAsync(
            queue: options.QueueName,
            exchange: options.ExchangeName,
            routingKey: bindRoutingKey,
            cancellationToken: cancellationToken);

        var prefetch = options.PrefetchCount == 0 ? (ushort)1 : options.PrefetchCount;

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: prefetch,
            global: false,
            cancellationToken: cancellationToken);
    }

    private static string ResolveBindRoutingKey(CallConsumerOptions options)
    {
        if (string.Equals(options.ExchangeType, "fanout", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return options.RoutingKey ?? string.Empty;
    }
}
