using HelperLib.Messaging;

namespace Contact_Center.Worker.Events.CallEvent.Options;

/// <summary>
/// Topology for publishing call-exchange events from the worker (used by the voicemail
/// AMI ingress). Must match the exchange the CallConsumer binds to, so the published
/// VoicemailRecorded event lands on the same queue.
/// </summary>
public sealed class CallEventPublisherOptions : IRabbitMqPublisherTopology
{
    public const string SectionName = "CallPublisher";

    public string ExchangeName { get; init; } = "voice.events";
    public string ExchangeType { get; init; } = "direct";
}
