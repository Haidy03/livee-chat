using VoiceFlow.Application.Interfaces;
using VoiceFlow.Contracts.Rbac;
using VoiceFlow.Core.Common;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Interfaces.Repositories;

namespace VoiceFlow.Application.Services;

public sealed class RbacService : IRbacService
{
    private readonly IRbacRoleRepository _roleRepository;
    private readonly IRbacUserRoleRepository _userRoleRepository;

    public RbacService(IRbacRoleRepository roleRepository, IRbacUserRoleRepository userRoleRepository)
    {
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
    }

    public async Task<Result<IEnumerable<RoleResponse>>> GetRolesAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetByTenantAsync(tenantId, cancellationToken);
        return Result.Success(roles.Select(MapToResponse));
    }

    public async Task<Result<RoleResponse>> GetRoleAsync(string roleId, string tenantId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null || (role.TenantId is not null && role.TenantId != tenantId))
            return Result.Failure<RoleResponse>(Error.NotFound("RbacRole", roleId));

        return MapToResponse(role);
    }

    public async Task<Result<RoleResponse>> CreateRoleAsync(string tenantId, CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = new RbacRole
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            Status = "active",
            IsSystem = false,
            Permissions = request.Permissions
        };

        await _roleRepository.InsertAsync(role, cancellationToken);
        return MapToResponse(role);
    }

    public async Task<Result<RoleResponse>> UpdateRoleAsync(string roleId, string tenantId, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null )
            return Result.Failure<RoleResponse>(Error.NotFound("RbacRole", roleId));
        //if (role.TenantId != tenantId)
        //{
        //    if (role.TenantId != tenantId)
        //    {
        //        var x = role.TenantId ?? string.Empty +"##";
        //        x += tenantId ?? string.Empty;
        //        return Result.Failure<RoleResponse>(Error.NotFound("tenantId", x));
        //    }
           
        //}

        if (role.IsSystem)
            return Result.Failure<RoleResponse>(Error.Forbidden("System roles cannot be modified."));

        if (request.Name is not null) role.Name = request.Name;
        if (request.Description is not null) role.Description = request.Description;
        if (request.Status is not null) role.Status = request.Status;
        if (request.Permissions is not null) role.Permissions = request.Permissions;

        await _roleRepository.UpdateAsync(role, cancellationToken);
        return MapToResponse(role);
    }

    public async Task<Result> DeleteRoleAsync(string roleId, string tenantId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role is null || role.TenantId != tenantId)
            return Result.Failure(Error.NotFound("RbacRole", roleId));

        if (role.IsSystem)
            return Result.Failure(Error.Forbidden("System roles cannot be deleted."));

        await _roleRepository.DeleteAsync(roleId, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<UserRoleResponse>> AssignRoleAsync(string tenantId, AssignRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
            return Result.Failure<UserRoleResponse>(Error.NotFound("RbacRole", request.RoleId));

        var userRole = new RbacUserRole
        {
            UserId = request.UserId,
            RoleId = request.RoleId,
            TenantId = tenantId
        };

        await _userRoleRepository.InsertAsync(userRole, cancellationToken);

        return new UserRoleResponse
        {
            UserId = request.UserId,
            RoleId = request.RoleId,
            RoleName = role.Name,
            AssignedAt = userRole.CreatedAt
        };
    }

    public async Task<Result> UnassignRoleAsync(string userId, string roleId, string tenantId, CancellationToken cancellationToken = default)
    {
        await _userRoleRepository.DeleteByUserAndTenantAsync(userId, tenantId, roleId, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<UserRoleResponse>>> GetUserRolesAsync(string userId, string tenantId, CancellationToken cancellationToken = default)
    {
        var userRoles = await _userRoleRepository.GetByUserAndTenantAsync(userId, tenantId, cancellationToken);
        var responses = new List<UserRoleResponse>();

        foreach (var ur in userRoles)
        {
            var role = await _roleRepository.GetByIdAsync(ur.RoleId, cancellationToken);
            responses.Add(new UserRoleResponse
            {
                UserId = ur.UserId,
                RoleId = ur.RoleId,
                RoleName = role?.Name ?? string.Empty,
                AssignedAt = ur.CreatedAt
            });
        }

        return Result.Success<IEnumerable<UserRoleResponse>>(responses);
    }

    public async Task<Result<IEnumerable<UserRoleResponse>>> GetAssignmentsAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var userRoles = await _userRoleRepository.GetByTenantAsync(tenantId, cancellationToken);
        var responses = new List<UserRoleResponse>();

        foreach (var ur in userRoles)
        {
            var role = await _roleRepository.GetByIdAsync(ur.RoleId, cancellationToken);
            responses.Add(new UserRoleResponse
            {
                UserId = ur.UserId,
                RoleId = ur.RoleId,
                RoleName = role?.Name ?? string.Empty,
                AssignedAt = ur.CreatedAt
            });
        }

        return Result.Success<IEnumerable<UserRoleResponse>>(responses);
    }

    private static RoleResponse MapToResponse(RbacRole role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        Status = role.Status,
        IsSystem = role.IsSystem,
        Permissions = role.Permissions
    };
}
