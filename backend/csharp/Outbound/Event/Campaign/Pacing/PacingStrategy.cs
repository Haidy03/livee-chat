using Outbound.Event.Campaign.Models;

namespace Outbound.Event.Campaign.Pacing;

/// <summary>Inputs a pacing strategy sees each sweep for one campaign.</summary>
public sealed record PacingContext(CampaignModel Campaign, int FreeAgents, long InFlight, CampaignStats Stats);

/// <summary>
/// Per-sweep pacing decision: given the campaign context, how many targets may be released to MAIN.
/// </summary>
public interface IPacingStrategy
{
    int ComputeReleaseBudget(PacingContext ctx);
}

/// <summary>One line per free agent — zero over-dial, so effectively no abandonment.</summary>
public sealed class ProgressiveStrategy : IPacingStrategy
{
    public int ComputeReleaseBudget(PacingContext ctx) => Math.Max(0, ctx.FreeAgents);
}

/// <summary>Fixed over-dial multiplier (<c>freeAgents × PowerRatio</c>).</summary>
public sealed class PowerStrategy : IPacingStrategy
{
    public int ComputeReleaseBudget(PacingContext ctx) =>
        (int)Math.Ceiling(Math.Max(0, ctx.FreeAgents) * (ctx.Campaign.PowerRatio <= 0 ? 1.0 : ctx.Campaign.PowerRatio));
}

/// <summary>Fixed budget with no agents (broadcast / IVR blast).</summary>
public sealed class AgentlessStrategy : IPacingStrategy
{
    private readonly int _budget;
    public AgentlessStrategy(int budget) { _budget = budget; }
    public int ComputeReleaseBudget(PacingContext ctx) => _budget;
}

/// <summary>
/// Adaptive over-dial: dial to fill free agents given the recent connect rate (an over-dial of
/// <c>1 / connectRate</c>), capped at <c>MaxOverdial</c>. Guarded by an abandonment cap — if the
/// recent abandon rate already exceeds <c>AbandonRateTarget</c> it drops to 1:1 (progressive) until
/// it recovers. On thin data <see cref="CampaignStats.Unknown"/> gives connect rate 1.0, so it
/// behaves like progressive until enough history accrues — never over-dials blind.
/// </summary>
public sealed class PredictiveStrategy : IPacingStrategy
{
    private const double DefaultAbandonTarget = 0.03;   // TCPA-style 3%
    private const double DefaultMinConnect = 0.20;
    private const double DefaultMaxOverdial = 3.0;

    public int ComputeReleaseBudget(PacingContext ctx)
    {
        var free = Math.Max(0, ctx.FreeAgents);
        if (free == 0) return 0;

        var c = ctx.Campaign;
        var abandonTarget = c.AbandonRateTarget > 0 ? c.AbandonRateTarget : DefaultAbandonTarget;
        var minConnect = c.MinConnectRate > 0 ? c.MinConnectRate : DefaultMinConnect;
        var maxOverdial = c.MaxOverdial > 1 ? c.MaxOverdial : DefaultMaxOverdial;

        // Safety first: if we're already abandoning too many, stop over-dialing.
        if (ctx.Stats.AbandonRate > abandonTarget) return free;

        var connect = ctx.Stats.ConnectRate <= 0 ? 1.0 : ctx.Stats.ConnectRate;
        connect = Math.Clamp(connect, minConnect, 1.0);
        var overdial = Math.Min(1.0 / connect, maxOverdial);
        return (int)Math.Ceiling(free * overdial);
    }
}

public interface IPacingStrategyFactory
{
    IPacingStrategy For(string? dialingMode);
}

public sealed class PacingStrategyFactory : IPacingStrategyFactory
{
    private readonly ProgressiveStrategy _progressive;
    private readonly PowerStrategy _power;
    private readonly AgentlessStrategy _agentless;
    private readonly PredictiveStrategy _predictive;

    public PacingStrategyFactory(ProgressiveStrategy p, PowerStrategy pw, AgentlessStrategy a, PredictiveStrategy pr)
    {
        _progressive = p; _power = pw; _agentless = a; _predictive = pr;
    }

    public IPacingStrategy For(string? dialingMode) => (dialingMode ?? "progressive").ToLowerInvariant() switch
    {
        "power" => _power,
        "agentless" => _agentless,
        "predictive" => _predictive,
        _ => _progressive,
    };
}
