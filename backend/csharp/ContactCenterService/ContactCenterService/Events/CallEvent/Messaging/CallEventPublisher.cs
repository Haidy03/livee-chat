using Contact_Center.Worker.Events.CallEvent.Options;
using HelperLib.Messaging;
using Microsoft.Extensions.Options;

namespace Contact_Center.Worker.Events.CallEvent.Messaging;

/// <summary>Publishes to the call exchange (voice.events). Used by the voicemail AMI ingress.</summary>
public sealed class CallEventPublisher : RabbitMqPublisherBase
{
    public CallEventPublisher(
        RabbitMqConnection connectionService,
        IOptions<CallEventPublisherOptions> topologyOptions,
        ILogger<CallEventPublisher> logger)
        : base(connectionService, topologyOptions.Value, logger)
    {
    }
}
