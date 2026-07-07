using Contact_Center.Worker.Events.CallEvent.Options;
using Contact_Center.Worker.Events.Reports.Options;
using HelperLib.Options;


namespace Contact_Center.Worker.Extensions
{
    internal static class AddOptionsExtension
    {
        internal static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CallConsumerOptions>(
                configuration.GetSection(CallConsumerOptions.SectionName));

            services.Configure<ReportConsumerOptions>(
                configuration.GetSection(ReportConsumerOptions.SectionName));

            services.Configure<CallAnalysisOptions>(
                configuration.GetSection(CallAnalysisOptions.SectionName));

            services.Configure<VoicemailAmiOptions>(
                configuration.GetSection(VoicemailAmiOptions.SectionName));

            services.Configure<CallEventPublisherOptions>(
                configuration.GetSection(CallEventPublisherOptions.SectionName));

            services
           .AddOptions<ReportPublisherOptions>()
           .BindConfiguration(ReportPublisherOptions.SectionName)
           .ValidateDataAnnotations()
           .ValidateOnStart();

            return services;
        }
    }
}
