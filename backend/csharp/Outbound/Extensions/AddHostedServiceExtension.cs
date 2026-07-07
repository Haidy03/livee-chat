using Outbound.Event.Campaign.Workers;
using Outbound.Infrastructure;
using Outbound.Infrastructure.Ami;

namespace Outbound.Extensions
{
    internal static class AddHostedServiceExtensions
    {
        internal static IServiceCollection AddHostedService(this IServiceCollection services)
        {
            // AMI: listener owns the socket; dispatcher fans events out to IAmiEventHandlers.
            services.AddHostedService<AmiListenerHostedService>();
            services.AddHostedService(sp => sp.GetRequiredService<AmiEventDispatcher>());

            // Single pull-based dispatcher + reaper — leader-gated.
            services.AddHostedService<LeaderWorkerHost<CampaignDispatcher>>();
            services.AddHostedService<LeaderWorkerHost<DialingReaper>>();

            return services;
        }
    }
}
