using VoiceFlow.Contracts.Rbac;
using VoiceFlow.Core.Common;

namespace VoiceFlow.Application.Interfaces;

public interface IRbacService
{
    Task<Result<IEnumerable<RoleResponse>>> GetRolesAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<Result<RoleResponse>> GetRoleAsync(string roleId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<RoleResponse>> CreateRoleAsync(string tenantId, CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result<RoleResponse>> UpdateRoleAsync(string roleId, string tenantId, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteRoleAsync(string roleId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<UserRoleResponse>> AssignRoleAsync(string tenantId, AssignRoleRequest request, CancellationToken cancellationToken = default);
    Task<Result> UnassignRoleAsync(string userId, string roleId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<UserRoleResponse>>> GetUserRolesAsync(string userId, string tenantId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<UserRoleResponse>>> GetAssignmentsAsync(string tenantId, CancellationToken cancellationToken = default);
}
