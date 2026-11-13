using System.Security.Cryptography;
using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Auth.Dtos;
using ChoyERP.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChoyERP.Application.Features.Auth;

/// <summary>
/// Autenticación con access token JWT y refresh token opaco con rotación.
/// </summary>
public sealed class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IAuditService auditService,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    IOptions<JwtOptions> jwtOptions)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = EmailNormalizer.Normalize(request.Email);
        var user = await LoadUserGraph()
            .SingleOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            // Se calcula un hash igualmente para no revelar por tiempo de respuesta si el correo existe.
            _ = passwordHasher.Hash(request.Password);
            await auditService.LogAsync(
                "auth.login.failed",
                details: $"Correo no registrado: {normalizedEmail}",
                cancellationToken: cancellationToken);
            throw new UnauthorizedAppException("Credenciales inválidas.");
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await auditService.LogAsync(
                "auth.login.failed",
                nameof(User),
                user.Id.ToString(),
                "Contraseña incorrecta.",
                user.Id,
                user.Email,
                cancellationToken);
            throw new UnauthorizedAppException("Credenciales inválidas.");
        }

        if (!user.IsActive)
        {
            await auditService.LogAsync(
                "auth.login.blocked",
                nameof(User),
                user.Id.ToString(),
                "Intento de acceso con cuenta desactivada.",
                user.Id,
                user.Email,
                cancellationToken);
            throw new ForbiddenAppException("La cuenta está desactivada. Contacte al administrador.");
        }

        var (roles, permissions) = ExtractAccess(user);
        var refreshToken = CreateRefreshToken(user);
        user.LastLoginAt = clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync(
            "auth.login.succeeded",
            nameof(User),
            user.Id.ToString(),
            userId: user.Id,
            userEmail: user.Email,
            cancellationToken: cancellationToken);

        return BuildResponse(user, roles, permissions, refreshToken);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = TokenHasher.Sha256(request.RefreshToken);
        var storedToken = await db.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.Worker)
            .Include(t => t.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            throw new UnauthorizedAppException("Token de actualización inválido.");
        }

        if (storedToken.RevokedAt is not null)
        {
            // Reutilización de un token ya rotado: se revoca toda la familia de tokens del usuario.
            await RevokeAllActiveTokensAsync(storedToken.UserId, "reuse_detected", cancellationToken);
            await auditService.LogAsync(
                "auth.refresh.reuse_detected",
                nameof(RefreshToken),
                storedToken.Id.ToString(),
                "Se detectó reutilización de un refresh token; se revocaron todas las sesiones.",
                storedToken.UserId,
                storedToken.User.Email,
                cancellationToken);
            throw new UnauthorizedAppException(
                "El token de actualización ya fue utilizado. Inicie sesión nuevamente.");
        }

        if (storedToken.ExpiresAt <= clock.UtcNow)
        {
            throw new UnauthorizedAppException("El token de actualización expiró. Inicie sesión nuevamente.");
        }

        if (!storedToken.User.IsActive)
        {
            throw new ForbiddenAppException("La cuenta está desactivada. Contacte al administrador.");
        }

        storedToken.RevokedAt = clock.UtcNow;
        storedToken.RevokedByIp = currentUser.IpAddress;
        storedToken.RevokedReason = "rotated";

        var newRefreshToken = CreateRefreshToken(storedToken.User);
        storedToken.ReplacedByTokenHash = newRefreshToken.Token.TokenHash;

        await db.SaveChangesAsync(cancellationToken);

        var (roles, permissions) = ExtractAccess(storedToken.User);
        return BuildResponse(storedToken.User, roles, permissions, newRefreshToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();

        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await RevokeAllActiveTokensAsync(userId, "logout", cancellationToken);
        }
        else
        {
            var tokenHash = TokenHasher.Sha256(request.RefreshToken);
            var storedToken = await db.RefreshTokens
                .SingleOrDefaultAsync(t => t.TokenHash == tokenHash && t.UserId == userId, cancellationToken);

            if (storedToken is not null && storedToken.RevokedAt is null)
            {
                storedToken.RevokedAt = clock.UtcNow;
                storedToken.RevokedByIp = currentUser.IpAddress;
                storedToken.RevokedReason = "logout";
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        await auditService.LogAsync(
            "auth.logout",
            nameof(User),
            userId.ToString(),
            userId: userId,
            userEmail: currentUser.Email,
            cancellationToken: cancellationToken);
    }

    public async Task<AuthenticatedUserDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var user = await LoadUserGraph()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAppException("La sesión no es válida.");

        if (!user.IsActive)
        {
            throw new ForbiddenAppException("La cuenta está desactivada. Contacte al administrador.");
        }

        var (roles, permissions) = ExtractAccess(user);
        return user.ToAuthenticatedUserDto(roles, permissions);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!PasswordPolicy.IsValid(request.NewPassword))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                [nameof(request.NewPassword)] = [PasswordPolicy.Description],
            });
        }

        var userId = currentUser.RequireUserId();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAppException("La sesión no es válida.");

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                [nameof(request.CurrentPassword)] = ["La contraseña actual no es correcta."],
            });
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await RevokeAllActiveTokensAsync(userId, "password_changed", cancellationToken);

        await auditService.LogAsync(
            "auth.password_changed",
            nameof(User),
            userId.ToString(),
            userId: userId,
            userEmail: user.Email,
            cancellationToken: cancellationToken);
    }

    private IQueryable<User> LoadUserGraph() =>
        db.Users
            .Include(u => u.Worker)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission);

    private static (string[] Roles, string[] Permissions) ExtractAccess(User user)
    {
        var roles = user.UserRoles
            .Select(ur => ur.Role.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var permissions = user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return (roles, permissions);
    }

    private (RefreshToken Token, string RawToken) CreateRefreshToken(User user)
    {
        var now = clock.UtcNow;
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

        var token = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = TokenHasher.Sha256(rawToken),
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays),
            CreatedByIp = currentUser.IpAddress,
        };

        db.RefreshTokens.Add(token);
        return (token, rawToken);
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var activeTokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
            token.RevokedByIp = currentUser.IpAddress;
            token.RevokedReason = reason;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private LoginResponse BuildResponse(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        (RefreshToken Token, string RawToken) refreshToken)
    {
        var accessToken = jwtTokenService.CreateAccessToken(user, roles, permissions);

        return new LoginResponse(
            accessToken.Value,
            accessToken.ExpiresAt,
            refreshToken.RawToken,
            refreshToken.Token.ExpiresAt,
            user.ToAuthenticatedUserDto(roles, permissions));
    }
}
