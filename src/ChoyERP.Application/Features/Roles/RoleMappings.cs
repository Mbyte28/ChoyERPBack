using ChoyERP.Application.Features.Roles.Dtos;
using ChoyERP.Domain.Entities.Users;

namespace ChoyERP.Application.Features.Roles;

internal static class RoleMappings
{
    public static PermissionDto ToDto(this Permission permission) =>
        new(permission.Id, permission.Code, permission.Name, permission.Module, permission.Description);

    public static RoleDto ToDto(this Role role) =>
        new(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.RolePermissions
                .Select(rp => rp.Permission.ToDto())
                .OrderBy(p => p.Module, StringComparer.OrdinalIgnoreCase)
                .ThenBy(p => p.Code, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            role.UserRoles.Count);
}
