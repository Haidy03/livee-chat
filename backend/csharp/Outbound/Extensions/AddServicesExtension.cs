using Microsoft.Extensions.Options;
using Outbound.Event.Campaign.Actions;
using Outbound.Event.Campaign.Ami;
using Outbound.Event.Campaign.Lookups;
using Outbound.Event.Campaign.Options;
using Outbound.Event.Campaign.Pacing;
using Outbound.Event.Campaign.Persistence;
using Outbound.Event.Campaign.RateLimiting;
using Outbound.Event.Campaign.Workers;
using Outbound.Infrastructure.Ami;

namespace Outbound.Extensions
{
    internal static class AddServicesExtension
    {
        internal static IServiceCollection AddServices(this IServiceCollection services)
        {
            // RedisSentinelConnectionFactory is registered by HelperLib.AddHelperServices.

            // Persistence + lookups
            services.AddSingleton<CampaignRepository>();
            services.AddSingleton<ICallAttemptRepository, CallAttemptRepository>();
            services.AddSingleton<ITenantTrunkRepository, TenantTrunkRepository>();
            services.AddSingleton<ICampaignLookupRepository, CampaignLookupRepository>();
            services.AddSingleton<IAgentLookupRepository, AgentLookupRepository>();

            // AMI infrastructure
            services.AddSingleton<IAmiMessageParser, AmiMessageParser>();
            services.AddSingleton<AmiActionSender>();
            services.AddSingleton<IAmiActionSender>(sp => sp.GetRequiredService<AmiActionSender>());
            services.AddSingleton<AmiConnectionStatus>();
            services.AddSingleton<AmiEventDispatcher>();
            services.AddSingleton<IAmiEventDispatcher>(sp => sp.GetRequiredService<AmiEventDispatcher>());

            // Concurrency + rate limiting (Redis-backed)
            services.AddSingleton<IConcurrencyCounter, ConcurrencyCounter>();
            services.AddSingleton<CampaignRateLimiter>();

            // Agent availability — register both, then select via config.
            services.AddSingleton<RedisAgentAvailabilityTracker>();
            services.AddSingleton<AgentAvailabilityTracker>();
            services.AddSingleton<IAgentAvailabilityTracker>(sp =>
            {
                var mode = sp.GetRequiredService<IOptions<AgentAvailabilityOptions>>().Value.Source;
                return string.Equals(mode, "ami", StringComparison.OrdinalIgnoreCase)
                    ? sp.GetRequiredService<AgentAvailabilityTracker>()
                    : sp.GetRequiredService<RedisAgentAvailabilityTracker>();
            });
            // The AMI tracker is still registered as an IAmiEventHandler so its state stays warm
            // and can be used as a fallback without a restart.
            services.AddSingleton<IAmiEventHandler>(sp => sp.GetRequiredService<AgentAvailabilityTracker>());

            // Pacing strategies + factory
            services.AddSingleton<ProgressiveStrategy>();
            services.AddSingleton<PowerStrategy>();
            services.AddSingleton(new AgentlessStrategy(budget: 10));
            services.AddSingleton<PredictiveStrategy>();
            services.AddSingleton<ICampaignStatsProvider, CampaignStatsProvider>();
            services.AddSingleton<IPacingStrategyFactory, PacingStrategyFactory>();

            // Attempt registry (replaces the TCS-based correlator)
            services.AddSingleton<IAttemptRegistry, AttemptRegistry>();

            // Outcome finalize path
            services.AddSingleton<IOutcomeFinalizer, OutcomeFinalizer>();
            services.AddSingleton<IAmiEventHandler, OriginateResponseHandler>();
            services.AddSingleton<IAmiEventHandler, DialEndHandler>();
            services.AddSingleton<IAmiEventHandler, AgentCompleteHandler>();
            services.AddSingleton<IAmiEventHandler, HangupHandler>();
            services.AddSingleton<IAmiEventHandler, UserEventHandler>();

            // Originate (fire-and-forget)
            services.AddSingleton<IOriginator, AsteriskOriginator>();

            // Dispatcher + reaper (leader-gated singletons; both host and dispatcher-signal
            // resolve the same instance so the finalizer's Nudge wakes the dispatcher).
            services.AddSingleton<CampaignDispatcher>();
            services.AddSingleton<IDispatcherSignal>(sp => sp.GetRequiredService<CampaignDispatcher>());
            services.AddSingleton<DialingReaper>();

            return services;
        }
    }
}
