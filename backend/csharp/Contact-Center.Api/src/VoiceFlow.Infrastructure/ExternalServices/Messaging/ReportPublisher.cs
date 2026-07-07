using HelperLib.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoiceFlow.Application.Interfaces.Messaging;
using VoiceFlow.Infrastructure.Options;

namespace VoiceFlow.Infrastructure.ExternalServices.Messaging
{
    internal sealed class ReportPublisher : RabbitMqPublisherBase , IReportPublisher
    {
        public ReportPublisher(
            RabbitMqConnection connectionService,
            IOptions<ReportPublisherOptions> topologyOptions,
            ILogger<ReportPublisher> logger)
            : base(connectionService, topologyOptions.Value, logger)
        {
        }
    }
}
