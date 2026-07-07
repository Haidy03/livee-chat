using HelperLib.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceFlow.Application.Interfaces.Messaging;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Infrastructure.ExternalServices.Messaging;

/// <summary>
/// Publishes call-related messages to the exchange and routing key from <see cref="CallPublisherOptions"/>.
/// </summary>
public sealed class CallPublisher : RabbitMqPublisherBase, ICallPublisher
{
    public CallPublisher(
        RabbitMqConnection connectionService,
        IOptions<CallPublisherOptions> topologyOptions,
        ILogger<CallPublisher> logger)
        : base(connectionService, topologyOptions.Value, logger)
    {
    }
}
