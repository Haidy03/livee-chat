using Contact_Center.Worker.Events.CallEvent.Handlers;
using Contact_Center.Worker.Events.CallEvent.Messaging;
using Contact_Center.Worker.Events.CallEvent.Services;
using Contact_Center.Worker.Events.Reports.Handlers;
using Contact_Center.Worker.Events.Reports.Messaging;
using Contact_Center.Worker.Events.Reports.Services;
using Contact_Center.Worker.Events.Reports.Services.Execution;
using HelperLib.Messaging;


namespace Contact_Center.Worker.Extensions
{
    internal static class AddServicesExtension
    {
        internal static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton < CallMqConsumer>();
            services.AddSingleton <ReportMqConsumer>();
            services.AddScoped<CallTermenatedProcessingService>();
            services.AddScoped<CallTerminatedEventHandler>();
            services.AddScoped<CallAnalysisService>();
            services.AddScoped<ReportHandler>();
            services.AddScoped<ReportRunner>();
            services.AddScoped<ReportExecutor>();
            services.AddSingleton<ReportPublisher>();
            services.AddSingleton<CallEventPublisher>();
            return services;
        }
    }
}
