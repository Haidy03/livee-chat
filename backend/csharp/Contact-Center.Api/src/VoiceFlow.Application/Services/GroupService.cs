using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Groups;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;

    public GroupService(IGroupRepository groupRepository)
    {
        _groupRepository = groupRepository;
    }

    public async Task<Result<IEnumerable<GroupResponse>>> GetGroupsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var groups = await _groupRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result.Success(groups.Select(MapToResponse));
    }

    public async Task<Result<GroupResponse>> GetGroupAsync(string groupId, string tenantId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is null || group.TenantId != tenantId)
            return Result.Failure<GroupResponse>(Error.NotFound("Group", groupId));

        return MapToResponse(group);
    }

    public async Task<Result<GroupResponse>> CreateGroupAsync(string tenantId, CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = new Group
        {
            TenantId = tenantId,
            Name = request.Name,
            RingStrategy = request.RingStrategy,
            RingTimeout = request.RingTimeout,
            Members = request.Members
        };

        await _groupRepository.InsertAsync(group, cancellationToken);
        return MapToResponse(group);
    }

    public async Task<Result<GroupResponse>> UpdateGroupAsync(string groupId, string tenantId, UpdateGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is null || group.TenantId != tenantId)
            return Result.Failure<GroupResponse>(Error.NotFound("Group", groupId));

        if (request.Name is not null) group.Name = request.Name;
        if (request.RingStrategy.HasValue) group.RingStrategy = request.RingStrategy.Value;
        if (request.RingTimeout.HasValue) group.RingTimeout = request.RingTimeout.Value;
        if (request.Members is not null) group.Members = request.Members;

        await _groupRepository.UpdateAsync(group, cancellationToken);
        return MapToResponse(group);
    }

    public async Task<Result> DeleteGroupAsync(string groupId, string tenantId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken);
        if (group is null || group.TenantId != tenantId)
            return Result.Failure(Error.NotFound("Group", groupId));

        await _groupRepository.DeleteAsync(groupId, cancellationToken);
        return Result.Success();
    }

    private static GroupResponse MapToResponse(Group group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        Members = group.Members,
        RingStrategy = group.RingStrategy,
        RingTimeout = group.RingTimeout,
        ActiveCalls = group.ActiveCalls,
        CreatedAt = group.CreatedAt,
        UpdatedAt = group.UpdatedAt
    };
}
