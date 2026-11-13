using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Users.Dtos;
using ChoyERP.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Users;

public sealed class UserService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IAuditService auditService,
    ICurrentUserService currentUser,
    IDateTimeProvider clock)
{
    public async Task<PagedResult<UserDto>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken)
    {
        var users = db.Users
            .AsNoTracking()
            .Include(u => u.Worker)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            users = users.Where(u =>
                u.NormalizedEmail.Contains(search)
                || (u.Worker != null
                    && (u.Worker.FirstName.ToLower().Contains(search)
                        || u.Worker.LastName.ToLower().Contains(search))));
        }

        if (query.IsActive is not null)
        {
            users = users.Where(u => u.IsActive == query.IsActive);
        }

        if (query.RoleId is not null)
        {
            users = users.Where(u => u.UserRoles.Any(ur => ur.RoleId == query.RoleId));
        }

        users = (query.SortBy?.ToLowerInvariant()) switch
        {
            "createdat" => query.SortDesc
                ? users.OrderByDescending(u => u.CreatedAt)
                : users.OrderBy(u => u.CreatedAt),
            "lastloginat" => query.SortDesc
                ? users.OrderByDescending(u => u.LastLoginAt)
                : users.OrderBy(u => u.LastLoginAt),
            "isactive" => query.SortDesc
                ? users.OrderByDescending(u => u.IsActive)
                : users.OrderBy(u => u.IsActive),
            _ => query.SortDesc
                ? users.OrderByDescending(u => u.Email)
                : users.OrderBy(u => u.Email),
        };

        var page = await users.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<UserDto>(
            page.Items.Select(u => u.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetUserOrThrowAsync(id, cancellationToken);
        return user.ToDto();
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailNormalizer.Normalize(request.Email);

        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("Ya existe un usuario con ese correo.");
        }

        var roles = await ResolveRolesAsync(request.RoleIds, cancellationToken);
        await EnsureWorkerIsAvailableAsync(request.WorkerId, null, cancellationToken);

        var user = new User
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            IsActive = request.IsActive,
            WorkerId = request.WorkerId,
        };

        foreach (var role in roles)
        {
            user.UserRoles.Add(new UserRole { Role = role });
        }

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "users.created",
            nameof(User),
            user.Id.ToString(),
            details: $"Correo: {user.Email}. Roles: {string.Join(", ", roles.Select(r => r.Name))}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await GetUserOrThrowAsync(id, cancellationToken);
        var normalizedEmail = EmailNormalizer.Normalize(request.Email);

        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != id, cancellationToken))
        {
            throw new ConflictException("Ya existe un usuario con ese correo.");
        }

        var roles = await ResolveRolesAsync(request.RoleIds, cancellationToken);
        await EnsureWorkerIsAvailableAsync(request.WorkerId, id, cancellationToken);

        var isDeactivating = user.IsActive && !request.IsActive;
        if (isDeactivating && user.Id == currentUser.UserId)
        {
            throw new ConflictException("No puede desactivar su propia cuenta.");
        }

        user.Email = request.Email.Trim();
        user.NormalizedEmail = normalizedEmail;
        user.WorkerId = request.WorkerId;
        user.IsActive = request.IsActive;

        var requestedRoleIds = roles.Select(r => r.Id).ToHashSet();
        foreach (var userRole in user.UserRoles.Where(ur => !requestedRoleIds.Contains(ur.RoleId)).ToList())
        {
            user.UserRoles.Remove(userRole);
        }

        foreach (var role in roles.Where(r => user.UserRoles.All(ur => ur.RoleId != r.Id)))
        {
            user.UserRoles.Add(new UserRole { Role = role });
        }

        if (isDeactivating)
        {
            await RevokeActiveTokensAsync(user.Id, "user_deactivated", cancellationToken);
        }
        else
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        await auditService.LogAsync(
            "users.updated",
            nameof(User),
            user.Id.ToString(),
            details: $"Correo: {user.Email}. Roles: {string.Join(", ", roles.Select(r => r.Name))}. Activo: {user.IsActive}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserDto> SetStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var user = await GetUserOrThrowAsync(id, cancellationToken);

        if (!isActive && user.Id == currentUser.UserId)
        {
            throw new ConflictException("No puede desactivar su propia cuenta.");
        }

        if (user.IsActive == isActive)
        {
            return user.ToDto();
        }

        user.IsActive = isActive;

        if (!isActive)
        {
            await RevokeActiveTokensAsync(user.Id, "user_deactivated", cancellationToken);
        }
        else
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        await auditService.LogAsync(
            isActive ? "users.activated" : "users.deactivated",
            nameof(User),
            user.Id.ToString(),
            cancellationToken: cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword, CancellationToken cancellationToken)
    {
        if (!PasswordPolicy.IsValid(newPassword))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["newPassword"] = [PasswordPolicy.Description],
            });
        }

        var user = await GetUserOrThrowAsync(id, cancellationToken);
        user.PasswordHash = passwordHasher.Hash(newPassword);

        await RevokeActiveTokensAsync(user.Id, "password_reset", cancellationToken);

        await auditService.LogAsync(
            "users.password_reset",
            nameof(User),
            user.Id.ToString(),
            details: "Un administrador restableció la contraseña.",
            cancellationToken: cancellationToken);
    }

    private async Task<User> GetUserOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Users
            .Include(u => u.Worker)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken)
        ?? throw new NotFoundException("Usuario", id);

    private async Task<List<Role>> ResolveRolesAsync(
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        var ids = roleIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["Debe asignar al menos un rol."],
            });
        }

        var roles = await db.Roles.Where(r => ids.Contains(r.Id)).ToListAsync(cancellationToken);

        if (roles.Count != ids.Length)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["roleIds"] = ["Uno o más roles no existen."],
            });
        }

        return roles;
    }

    private async Task EnsureWorkerIsAvailableAsync(
        Guid? workerId,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        if (workerId is null)
        {
            return;
        }

        var worker = await db.Workers
            .SingleOrDefaultAsync(w => w.Id == workerId, cancellationToken);

        if (worker is null)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["workerId"] = ["El trabajador indicado no existe."],
            });
        }

        if (!worker.IsActive)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["workerId"] = ["El trabajador indicado está desactivado."],
            });
        }

        var alreadyLinked = await db.Users.AnyAsync(
            u => u.WorkerId == workerId && u.Id != excludeUserId,
            cancellationToken);

        if (alreadyLinked)
        {
            throw new ConflictException("El trabajador ya tiene una cuenta de usuario asignada.");
        }
    }

    private async Task RevokeActiveTokensAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = currentUser.IpAddress;
            token.RevokedReason = reason;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
