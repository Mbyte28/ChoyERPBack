using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Features.Roles.Dtos;
using ChoyERP.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Roles;

public sealed class RoleService(IAppDbContext db, IAuditService auditService)
{
    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(r => r.ToDto()).ToArray();
    }

    public async Task<RoleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await GetRoleOrThrowAsync(id, cancellationToken);
        return role.ToDto();
    }

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(request.Name);

        if (await db.Roles.AnyAsync(r => r.NormalizedName == normalizedName, cancellationToken))
        {
            throw new ConflictException("Ya existe un rol con ese nombre.");
        }

        var permissions = await ResolvePermissionsAsync(request.PermissionCodes, cancellationToken);

        var role = new Role
        {
            Name = request.Name.Trim(),
            NormalizedName = normalizedName,
            Description = request.Description?.Trim(),
            IsSystemRole = false,
        };

        foreach (var permission in permissions)
        {
            role.RolePermissions.Add(new RolePermission { Permission = permission });
        }

        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "roles.created",
            nameof(Role),
            role.Id.ToString(),
            details: $"Rol: {role.Name}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(role.Id, cancellationToken);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = await GetRoleOrThrowAsync(id, cancellationToken);

        if (role.IsSystemRole)
        {
            throw new ConflictException("Los roles del sistema no se pueden modificar.");
        }

        var normalizedName = NormalizeName(request.Name);

        if (await db.Roles.AnyAsync(r => r.NormalizedName == normalizedName && r.Id != id, cancellationToken))
        {
            throw new ConflictException("Ya existe un rol con ese nombre.");
        }

        var permissions = await ResolvePermissionsAsync(request.PermissionCodes, cancellationToken);

        role.Name = request.Name.Trim();
        role.NormalizedName = normalizedName;
        role.Description = request.Description?.Trim();

        var requestedPermissionIds = permissions.Select(p => p.Id).ToHashSet();
        foreach (var rolePermission in role.RolePermissions
                     .Where(rp => !requestedPermissionIds.Contains(rp.PermissionId))
                     .ToList())
        {
            role.RolePermissions.Remove(rolePermission);
        }

        foreach (var permission in permissions.Where(p => role.RolePermissions.All(rp => rp.PermissionId != p.Id)))
        {
            role.RolePermissions.Add(new RolePermission { Permission = permission });
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "roles.updated",
            nameof(Role),
            role.Id.ToString(),
            details: $"Rol: {role.Name}. Permisos: {string.Join(", ", permissions.Select(p => p.Code))}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(role.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await GetRoleOrThrowAsync(id, cancellationToken);

        if (role.IsSystemRole)
        {
            throw new ConflictException("Los roles del sistema no se pueden eliminar.");
        }

        if (role.UserRoles.Count > 0)
        {
            throw new ConflictException("El rol tiene usuarios asignados; reasígnelos antes de eliminarlo.");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "roles.deleted",
            nameof(Role),
            id.ToString(),
            details: $"Rol eliminado: {role.Name}.",
            cancellationToken: cancellationToken);
    }

    private async Task<Role> GetRoleOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .Include(r => r.UserRoles)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
        ?? throw new NotFoundException("Rol", id);

    private async Task<List<Permission>> ResolvePermissionsAsync(
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        var codes = permissionCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (codes.Length == 0)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["permissionCodes"] = ["Debe asignar al menos un permiso."],
            });
        }

        var permissions = await db.Permissions
            .Where(p => codes.Contains(p.Code))
            .ToListAsync(cancellationToken);

        if (permissions.Count != codes.Length)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["permissionCodes"] = ["Uno o más permisos no existen."],
            });
        }

        return permissions;
    }

    private static string NormalizeName(string name) => name.Trim().ToLowerInvariant();
}

public sealed class PermissionService(IAppDbContext db)
{
    public async Task<IReadOnlyList<PermissionDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var permissions = await db.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Code)
            .ToListAsync(cancellationToken);

        return permissions.Select(p => p.ToDto()).ToArray();
    }
}
