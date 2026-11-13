namespace ChoyERP.Application.Features.Roles.Dtos;

public sealed record PermissionDto(
    Guid Id,
    string Code,
    string Name,
    string Module,
    string? Description);

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    IReadOnlyList<PermissionDto> Permissions,
    int UserCount);

public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionCodes);

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyList<string> PermissionCodes);
