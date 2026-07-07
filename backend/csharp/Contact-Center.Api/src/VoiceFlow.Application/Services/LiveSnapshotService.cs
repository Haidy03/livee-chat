using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Live;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Enums;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class LiveSnapshotService : ILiveSnapshotService
{
    private readonly ICallRepository _callRepository;
    private readonly IProfileRepository _profileRepository;

    public LiveSnapshotService(ICallRepository callRepository, IProfileRepository profileRepository)
    {
        _callRepository = callRepository;
        _profileRepository = profileRepository;
    }

    public async Task<Result<LiveSnapshot>> GetSnapshotAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var activeEntities = await _callRepository.GetActiveCallsForTenantAsync(tenantId, cancellationToken);
        var activeCalls = activeEntities.Select(MapActiveCall).ToList().AsReadOnly();

        var inboundWaiting = activeEntities
            .Where(c => c.Direction == CallDirection.Inbound && c.AnsweredAt is null)
            .ToList();

        var queue = new QueueStats
        {
            Waiting = inboundWaiting.Count,
            LongestWaitSeconds = inboundWaiting.Count == 0
                ? 0
                : (int)Math.Clamp(
                    inboundWaiting.Max(c => (DateTime.UtcNow - c.StartedAt).TotalSeconds),
                    0,
                    int.MaxValue)
        };

        var profiles = await _profileRepository.GetByTenantAsync(tenantId, cancellationToken);
        var agents = profiles
            .Where(p => !p.Disabled)
            .Select(p => MapLiveAgent(p, activeEntities))
            .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();

        var snapshot = new LiveSnapshot
        {
            ActiveCalls = activeCalls,
            Queue = queue,
            Agents = agents
        };

        return Result.Success(snapshot);
    }

    private static ActiveCall MapActiveCall(Call c) => new()
    {
        Id = c.Id,
        Caller = c.Caller,
        Called = c.Called,
        AgentId = c.AgentId,
        GroupId = c.GroupId,
        StartedAt = c.StartedAt,
        AnsweredAt = c.AnsweredAt,
        Status = MapLiveCallStatus(c)
    };

    private static string MapLiveCallStatus(Call c)
    {
        if (c.AnsweredAt is null)
            return "ringing";
        if (c.HoldSeconds > 0)
            return "hold";
        return "answered";
    }

    private static LiveAgent MapLiveAgent(Profile profile, IReadOnlyList<Call> activeCalls)
    {
        var currentCall = activeCalls
            .Where(c =>
                string.Equals(c.AgentId, profile.UserId, StringComparison.Ordinal)
                || string.Equals(c.UserId, profile.UserId, StringComparison.Ordinal))
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefault();

        var normalized = NormalizeAgentPresenceStatus(profile.Status);
        var status = currentCall is not null
            ? (normalized == "break" ? "break" : "busy")
            : normalized;

        return new LiveAgent
        {
            UserId = profile.UserId,
            DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? profile.Email ?? profile.UserId
                : profile.DisplayName,
            Extension = profile.ExtensionNumber,
            Status = status,
            CurrentCallId = currentCall?.Id,
            LastChangeAt = profile.UpdatedAt
        };
    }

    private static string NormalizeAgentPresenceStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "offline";

        return status.Trim().ToLowerInvariant() switch
        {
            "online" => "online",
            "available" => "available",
            "busy" => "busy",
            "break" or "on_break" or "onbreak" => "break",
            "offline" => "offline",
            _ => "offline"
        };
    }
}
