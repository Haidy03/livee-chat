using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceFlow.Contracts.Reports
{
    public sealed class ReportFiltersDto
    {
        public string DateRange { get; set; } = "last_30_days";
        public string Channels { get; set; } = "all";
        public List<string> Agents { get; set; } = new();
        public List<string> Queues { get; set; } = new();
        public List<string> Skills { get; set; } = new();
    }
}
