using System.ComponentModel.DataAnnotations;

namespace Contact_Center.Worker.Events.CallEvent.Options;

/// <summary>
/// RabbitMQ topology for consuming call-related messages (exchange binding + this service's queue).
/// When adding another consumer type later, introduce an additional section or options type per worker.
/// </summary>
public sealed class CallConsumerOptions
{
    public const string SectionName = "CallConsumer";

    [Required]
    public string ExchangeName { get; init; } = "";

    [Required]
    public string ExchangeType { get; init; } = "direct";

    /// <summary>Queue bind routing key (not used for fanout exchanges).</summary>
    public string RoutingKey { get; init; } = "";

    [Required]
    public string QueueName { get; init; } = "";

    /// <summary>Optional prefetch; defaults to 1 when omitted or zero.</summary>
    [Range(0, ushort.MaxValue)]
    public ushort PrefetchCount { get; init; } = 1;
    public string AstriskPath { get; init; } = "";
    public string VoiceLibraryPath { get; init; } = "";
}
