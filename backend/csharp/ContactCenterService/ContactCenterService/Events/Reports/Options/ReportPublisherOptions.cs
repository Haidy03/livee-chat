using HelperLib.Messaging;
using System.ComponentModel.DataAnnotations;


namespace Contact_Center.Worker.Events.Reports.Options
{
    public class ReportPublisherOptions: IRabbitMqPublisherTopology
    {
        public const string SectionName = "ReportPublisher";
        [Required]
        public string ExchangeName { get; init; } = "voiceflow.events";
        public string ExchangeType { get; init; } = "direct";
    }
}
