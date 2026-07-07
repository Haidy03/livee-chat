using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceFlow.Contracts.Reports
{
    public sealed class ReportResponse
    {
        public string Id { get; set; } = string.Empty;
        public BiDto Name { get; set; } = new();
        public BiDto Description { get; set; } = new();
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public ReportDefinitionDto Definition { get; set; } = new();
        public ScheduleDto Schedule { get; set; } = new();
        public bool Starred { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
        public string? LastRunAt { get; set; }
        public int RunsCount { get; set; }
        public int RecipientsCount { get; set; }
    }
}
