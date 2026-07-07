using System.Collections.Generic;

namespace VoiceFlow.Contracts.Reports;

public sealed class DataSourceFieldDto
{
    public string Key { get; set; } = string.Empty;
    public BiDto Label { get; set; } = new();
    public string DataType { get; set; } = "string";
    public bool CanUseInDetail { get; set; }
    public bool CanUseAsDimension { get; set; }
    public bool CanFilter { get; set; }
    public bool CanSort { get; set; }
}

public sealed class DataSourceMetricDto
{
    public string Key { get; set; } = string.Empty;
    public BiDto Label { get; set; } = new();
    public string DataType { get; set; } = "number";
    public string Kind { get; set; } = string.Empty;
}

public sealed class DataSourceMetadataResponse
{
    public string DataSource { get; set; } = string.Empty;
    public BiDto Label { get; set; } = new();
    public BiDto Description { get; set; } = new();
    public string Icon { get; set; } = "Database";

    /// <summary>False for planned sources that have no data yet — the UI shows them disabled.</summary>
    public bool Ready { get; set; } = true;

    public List<string> SupportedModes { get; set; } = new() { "detail", "metricAndDimension" };
    public List<DataSourceFieldDto> Fields { get; set; } = new();
    public List<DataSourceMetricDto> Metrics { get; set; } = new();
}
