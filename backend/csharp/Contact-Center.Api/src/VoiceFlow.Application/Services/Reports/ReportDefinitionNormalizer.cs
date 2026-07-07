using System;
using System.Collections.Generic;
using System.Linq;
using VoiceFlow.Core.Reports.Catalog;
using VoiceFlow.Contracts.Reports;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;

namespace VoiceFlow.Application.Services.Reports;

/// <summary>
/// Normalises a report definition based on <see cref="ReportMode"/>:
/// strips fields irrelevant to the active mode, validates against the
/// data-source catalog and rejects unsafe field names. Called from
/// <see cref="ReportService"/> so bad payloads never reach persistence.
/// </summary>
public static class ReportDefinitionNormalizer
{
    private static readonly HashSet<char> UnsafeFieldChars = new() { '$', '\0' };

    public static (bool ok, string? error) Normalize(ReportDefinition def)
    {
        if (def is null) return (false, "definition_missing");
        if (string.IsNullOrWhiteSpace(def.DataSource))
            def.DataSource = "calls";

        var source = ReportDataSourceCatalog.Find(def.DataSource);
        if (source is null)
            return (false, $"unknown_data_source:{def.DataSource}");

        if (def.SchemaVersion <= 0) def.SchemaVersion = 2;

        if (def.Mode == ReportMode.Detail)
        {
            // Clear irrelevant fields
            def.Dimensions = new List<string>();
            def.Metrics = new List<string>();

            var cleaned = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in def.SelectedFields ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var key = raw.Trim();
                if (key.Any(c => UnsafeFieldChars.Contains(c)))
                    return (false, $"unsafe_field:{key}");
                var f = source.FindField(key);
                if (f is null || !f.CanUseInDetail)
                    return (false, $"unknown_field:{key}");
                if (seen.Add(f.Key)) cleaned.Add(f.Key);
            }
            if (cleaned.Count == 0)
                return (false, "detail_requires_selected_fields");
            def.SelectedFields = cleaned;

            if (def.Sort is not null && !string.IsNullOrWhiteSpace(def.Sort.Field))
            {
                var sf = source.FindField(def.Sort.Field);
                if (sf is null || !sf.CanSort)
                    return (false, $"unsortable_field:{def.Sort.Field}");
                def.Sort.Field = sf.Key;
            }
        }
        else // MetricAndDimension
        {
            def.SelectedFields = new List<string>();

            // Metric mode may delegate to another source's collection (agents → calls), so
            // dimensions/metrics are validated against the delegate when one is set.
            var metricSource = string.IsNullOrWhiteSpace(source.MetricDelegateKey)
                ? source
                : ReportDataSourceCatalog.Find(source.MetricDelegateKey) ?? source;

            var dims = new List<string>();
            foreach (var d in def.Dimensions ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(d)) continue;
                var key = d.Trim();
                // Special date grouping keys accepted directly.
                if (IsDateDimensionKey(key)) { dims.Add(key.ToLowerInvariant()); continue; }
                var f = metricSource.FindField(key);
                if (f is null || !f.CanUseAsDimension)
                    return (false, $"unknown_dimension:{key}");
                dims.Add(f.ResolvedKey);
            }
            def.Dimensions = dims.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var mets = new List<string>();
            foreach (var m in def.Metrics ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(m)) continue;
                var key = m.Trim();
                var mm = metricSource.FindMetric(key);
                // Backward compat: accept legacy avg_*/sum_*/min_*/max_* naming even if not in catalog.
                if (mm is null && !LooksLikeLegacyAgg(key))
                    return (false, $"unknown_metric:{key}");
                mets.Add(mm?.ResolvedKey ?? key);
            }
            def.Metrics = mets.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (def.Metrics.Count == 0)
                return (false, "metric_report_requires_metric");
        }

        return (true, null);
    }

    public static bool IsDateDimensionKey(string key) =>
        key.Replace("_", "").ToLowerInvariant() switch
        {
            "date" or "hour" or "dow" or "dayofweek" or "week" or "month" or "quarter" or "year" => true,
            _ => false,
        };

    private static bool LooksLikeLegacyAgg(string key)
    {
        var idx = key.IndexOf('_');
        if (idx <= 0) return false;
        var prefix = key.Substring(0, idx).ToLowerInvariant();
        return prefix is "avg" or "sum" or "min" or "max";
    }
}
