using Contact_Center.Worker.Events.Reports.constatnts;
using Contact_Center.Worker.Events.Reports.Helpers;
using Contact_Center.Worker.Events.Reports.Messaging;
using FirebaseAdmin.Auth.Multitenancy;
using Microsoft.CognitiveServices.Speech.Transcription;
using VoiceFlow.Contracts.Events;
using VoiceFlow.Core.Entities.Reports;
using VoiceFlow.Core.Enums.Reports;
using VoiceFlow.Core.Interfaces.Repositories.Reports;

namespace Contact_Center.Worker.Events.Reports.Workers
{
    public sealed class ReportSchedulerWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReportSchedulerWorker> _logger;
        public ReportSchedulerWorker(IServiceProvider serviceProvider , ILogger<ReportSchedulerWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();

                var repository =
                    scope.ServiceProvider.GetRequiredService<IReportRepository>();
                var _runRepo =
                    scope.ServiceProvider.GetRequiredService<IReportRunRepository>();
               
                var reportPublisher =
                    scope.ServiceProvider.GetRequiredService<ReportPublisher>();

                var dueReports =
                    await repository.GetDueReportsAsync(
                        DateTimeOffset.UtcNow,
                        stoppingToken);

               

                foreach (var report in dueReports)
                {
                    _logger.LogInformation(
                        $"Report {report.Id} is due to run at {report.NextRunAt.Value}");
                    //await QueueReportExecution(report); publish event to execute report
                    var now = DateTimeOffset.UtcNow;
                    var run = new ReportRun
                    {
                        TenantId = report.TenantId,
                        ReportId = report.Id,
                        StartedAt = now,
                        Status = ReportRunStatus.Queued,
                        TriggeredBy = report.OwnerId,
                        Trigger = ReportRunTrigger.Scheduled,
                    };
                    await _runRepo.AddAsync(run, stoppingToken);


                    await reportPublisher.PublishAsync(new ReportRunRequested
                    {
                        RunId = run.Id,
                        ReportId = report.Id,
                        TenantId = report.TenantId,
                        TriggeredBy = report.OwnerId,
                        Trigger = ReportRunTrigger.Scheduled,
                        RequestedAt = now,
                        CorrelationId = Guid.NewGuid().ToString("N"),
                    }, ReportRoutingKeys.Reports);

                    report.NextRunAt = ReportScheduleCalculator.CalculateNextRun(report.Schedule,DateTimeOffset.UtcNow);
                    report.LastRunAt = now;
                    report.RunsCount += 1;
                    report.UpdatedAt = now.DateTime;

                    await repository.UpdateReportAsync(
                        report,
                        stoppingToken);
                }

                await Task.Delay(
                    TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
        }
    }
}
