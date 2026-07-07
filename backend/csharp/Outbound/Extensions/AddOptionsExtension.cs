using HelperLib.Options;
using Outbound.Event.Campaign.Options;
using Outbound.Infrastructure.Ami;


namespace Outbound.Extensions
{
    internal static class AddOptionsExtension
    {
        internal static IServiceCollection ConfigureOptions(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<RabbitMqOptions>(
                configuration.GetSection(RabbitMqOptions.SectionName));

            services.Configure<CampaignRetryOptions>(
                configuration.GetSection(CampaignRetryOptions.SectionName));
            services.Configure<CampaignRateLimitOptions>(
                configuration.GetSection(CampaignRateLimitOptions.SectionName));

            // Pull-dispatcher options
            services.Configure<RedisKeyspaceOptions>(
                configuration.GetSection(RedisKeyspaceOptions.SectionName));
            services.Configure<AgentAvailabilityOptions>(
                configuration.GetSection(AgentAvailabilityOptions.SectionName));
            services.Configure<ConcurrencyOptions>(
                configuration.GetSection(ConcurrencyOptions.SectionName));
            services.Configure<DispatcherOptions>(
                configuration.GetSection(DispatcherOptions.SectionName));
            services.Configure<ReaperOptions>(
                configuration.GetSection(ReaperOptions.SectionName));

            // AMI + Asterisk
            services.Configure<AmiOptions>(configuration.GetSection(AmiOptions.SectionName));
            services.Configure<AsteriskOptions>(configuration.GetSection(AsteriskOptions.SectionName));

            return services;
        }
    }
}
