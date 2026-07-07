using HelperLib.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceFlow.Infrastructure.Options
{
    internal class ReportPublisherOptions : IRabbitMqPublisherTopology
    {
        public const string SectionName = "ReportPublisher";
        [Required]
        public string ExchangeName { get; init; } = "voiceflow.events";
        public string ExchangeType { get; init; } = "direct";

        [Required]
        public string RoutingKey { get; init; } = "reports";
    }
}
