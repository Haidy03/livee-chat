using Contact_Center.Worker.Events.CallEvent.Workers;
using Contact_Center.Worker.Events.Reports.Workers;

namespace Contact_Center.Worker.Extensions
{
    internal static class AddHostedServiceExtensions
    {
        internal static IServiceCollection AddHostedService(this IServiceCollection services)
        {
            services.AddHostedService<CallTerminatedEventConsumer>();
            services.AddHostedService<VoicemailAmiListener>();
            services.AddHostedService<CloseNonTerminatedCallsWorker>();
            services.AddHostedService<ReportSchedulerWorker>();
            services.AddHostedService<ReportProcessingEventConsumer>();
            return services;
        }
    }
}
