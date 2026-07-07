using HelperLib.Messaging;
using System.ComponentModel.DataAnnotations;
using VoiceFlow.Application.Interfaces.Messaging;

namespace VoiceFlow.Infrastructure.Options;

/// <summary>
/// Settings for the call-related RabbitMQ producer (configuration section <see cref="SectionName"/>).
/// </summary>
public sealed class CallPublisherOptions : IRabbitMqPublisherTopology
{
    public const string SectionName = "CallPublisher";

    [Required]
    public string ExchangeName { get; init; } = "voiceflow.events";

    /// <summary>
    /// RabbitMQ exchange type: direct, fanout, topic, headers
    /// </summary>
    public string ExchangeType { get; init; } = "direct";

    /// <summary>
    /// Routing key for <see cref="IMessageBus.PublishAsync{T}"/>; consumer apps bind their queues to this key.
    /// </summary>
    [Required]
    public string RoutingKey { get; init; } = "calls";
}
