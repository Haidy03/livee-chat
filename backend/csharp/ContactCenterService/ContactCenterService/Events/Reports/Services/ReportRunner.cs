using System.Diagnostics;
using Contact_Center.Worker.Events.Reports.Services.Execution;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Interfaces.Repositories.Reports;


namespace Contact_Center.Worker.Events.Reports.Services;
    public sealed class ReportRunner
    {
        private readonly IReportRepository _reports;
        private readonly IReportRunRepository _runs;
        private readonly IReportResultRepository _results;
        private readonly ReportExecutor _executor;
        private readonly ILogger<ReportRunner> _log;
        private readonly DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

        public ReportRunner(
            IReportRepository reports,
            IReportRunRepository runs,
            IReportResultRepository results,
            ReportExecutor executor,
            ILogger<ReportRunner> log)
        {
            _reports = reports; _runs = runs; _results = results;
            _executor = executor; _log = log;
        }

        public async Task RunAsync(ReportRunRequested message, CancellationToken ct)
        {
            var run = await _runs.GetAsync(message.RunId, ct);
            if (run is null)
            {
                _log.LogWarning("ReportRun {RunId} not found, dropping message", message.RunId);
                return;
            }

            var report = await _reports.GetAsync(message.TenantId, message.ReportId, ct);
            if (report is null)
            {
                run.Status = ReportRunStatus.Failed;
                run.FinishedAt = _utcNow;
                run.ErrorMessage = $"Report {message.ReportId} not found";
                await _runs.UpdateReportAsync(run, ct);
                return;
            }

            run.Status = ReportRunStatus.Running;
            run.StartedAt = _utcNow;
            run.Attempts += 1;
            await _runs.UpdateReportAsync(run, ct);

            var sw = Stopwatch.StartNew();
            try
            {
                var output = await _executor.ExecuteAsync(report, message, ct);

                var result = new ReportResult
                {
                    TenantId = report.TenantId,
                    ReportId = report.Id,
                    RunId = run.Id,
                    GeneratedAt = _utcNow,
                    Columns = output.Columns.ToList(),
                    Rows = output.Rows.ToList(),
                    Summary = output.Summary.ToDictionary(kv => kv.Key, kv => kv.Value),
                    RowCount = output.Rows.Count,
                };
                await _results.AddAsync(result, ct);

                sw.Stop();
                run.Status = ReportRunStatus.Succeeded;
                run.FinishedAt = _utcNow;
                run.DurationMs = sw.ElapsedMilliseconds;
                run.RowCount = result.RowCount;
                run.ResultId = result.Id;
                run.ErrorMessage = null;
                await _runs.UpdateReportAsync(run, ct);

                _log.LogInformation("Report {ReportId} run {RunId} succeeded in {Ms}ms with {Rows} rows",
                    report.Id, run.Id, sw.ElapsedMilliseconds, result.RowCount);
            }
            catch (Exception ex)
            {
                sw.Stop();
                run.Status = ReportRunStatus.Failed;
                run.FinishedAt = _utcNow;
                run.DurationMs = sw.ElapsedMilliseconds;
                run.ErrorMessage = ex.Message;
                await _runs.UpdateReportAsync(run, ct);
                _log.LogError(ex, "Report {ReportId} run {RunId} failed", report.Id, run.Id);
                throw; // surface to consumer for retry/DLQ decision
            }
        }
    }


