using VoiceFlow.Contracts.Groups;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IGroupService
{
    Task<Result<IEnumerable<GroupResponse>>> GetGroupsAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> GetGroupAsync(string groupId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> CreateGroupAsync(string tenantId, CreateGroupRequest request, CancellationToken cancellationToken = default);
    Task<Result<GroupResponse>> UpdateGroupAsync(string groupId, string tenantId, UpdateGroupRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteGroupAsync(string groupId, string tenantId, CancellationToken cancellationToken = default);
}
