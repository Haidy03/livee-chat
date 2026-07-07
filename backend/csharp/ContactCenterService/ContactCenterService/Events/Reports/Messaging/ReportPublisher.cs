using Contact_Center.Worker.Events.Reports.Options;
using HelperLib.Messaging;
using Microsoft.Extensions.Options;

namespace Contact_Center.Worker.Events.Reports.Messaging
{
    internal sealed class ReportPublisher : RabbitMqPublisherBase
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
