using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceFlow.Contracts.Reports
{
    public sealed class ScheduleDto
    {
        public string Frequency { get; set; } = "daily";
        public string RunTime { get; set; } = "08:00";
        public string Timezone { get; set; } = "UTC";
        public List<string> WeekDays { get; set; } = new();
        public List<int> MonthDays { get; set; } = new();
        public string? Cron { get; set; }
        public List<string> Recipients { get; set; } = new();
        public List<string> Formats { get; set; } = new();
        public bool Slack { get; set; }
        public bool Webhook { get; set; }
        public bool Sftp { get; set; }
        public bool Enabled { get; set; } = true;
    }
}
