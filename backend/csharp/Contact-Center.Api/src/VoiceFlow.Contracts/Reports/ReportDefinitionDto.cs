using System.Collections.Generic;

namespace VoiceFlow.Contracts.Reports
{
    public sealed class ReportSortDto
    {
        public string Field { get; set; } = string.Empty;
        public string Direction { get; set; } = "desc";
    }

    public sealed class ReportDefinitionDto
    {
        /// <summary>"detail" or "metricAndDimension". Defaults to metricAndDimension for legacy payloads.</summary>
        public string Mode { get; set; } = "metricAndDimension";

        public string DataSource { get; set; } = string.Empty;

        public List<string> SelectedFields { get; set; } = new();

        public List<string> Metrics { get; set; } = new();

        public List<string> Dimensions { get; set; } = new();

        public ReportFiltersDto Filters { get; set; } = new();

        public ReportSortDto? Sort { get; set; }

        public string Visualization { get; set; } = "table";

        public int SchemaVersion { get; set; } = 2;
    }
}
