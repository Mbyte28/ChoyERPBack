using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Auth;
using ChoyERP.Application.Features.Auth.Dtos;
using ChoyERP.Domain.Entities.Users;
using ChoyERP.Infrastructure.Identity;
using ChoyERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.UnitTests.Features;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndPermissions()
    {
        var sut = CreateSut(out var db, out var user, out var audit);

        var result = await sut.LoginAsync(
            new LoginRequest("USER@choy.local", "Password123"),
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Contains(Permissions.Clients.Read, result.User.Permissions);
        Assert.Equal("Comercial", Assert.Single(result.User.Roles));
        Assert.NotNull(user.LastLoginAt);
        Assert.Contains("auth.login.succeeded", audit.Actions);

        var storedToken = await db.RefreshTokens.SingleAsync();
        Assert.Equal(TokenHasher.Sha256(result.RefreshToken), storedToken.TokenHash);
        Assert.Null(storedToken.RevokedAt);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        var sut = CreateSut(out _, out _, out var audit);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            sut.LoginAsync(new LoginRequest("user@choy.local", "Incorrecta123"), CancellationToken.None));

        Assert.Contains("auth.login.failed", audit.Actions);
    }

    [Fact]
    public async Task Login_WithInactiveUser_ThrowsForbidden()
    {
        var sut = CreateSut(out var db, out var user, out _);
        user.IsActive = false;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            sut.LoginAsync(new LoginRequest("user@choy.local", "Password123"), CancellationToken.None));
    }

    [Fact]
    public async Task Refresh_RotatesTokenAndRevokesPrevious()
    {
        var sut = CreateSut(out var db, out _, out _);

        var login = await sut.LoginAsync(
            new LoginRequest("user@choy.local", "Password123"),
            CancellationToken.None);

        var refreshed = await sut.RefreshAsync(
            new RefreshTokenRequest(login.RefreshToken),
            CancellationToken.None);

        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);

        var previous = await db.RefreshTokens.SingleAsync(
            t => t.TokenHash == TokenHasher.Sha256(login.RefreshToken));

        Assert.NotNull(previous.RevokedAt);
        Assert.Equal("rotated", previous.RevokedReason);
        Assert.Equal(TokenHasher.Sha256(refreshed.RefreshToken), previous.ReplacedByTokenHash);
    }

    [Fact]
    public async Task Refresh_ReusingRevokedToken_RevokesAllSessions()
    {
        var sut = CreateSut(out var db, out _, out var audit);

        var login = await sut.LoginAsync(
            new LoginRequest("user@choy.local", "Password123"),
            CancellationToken.None);

        await sut.RefreshAsync(new RefreshTokenRequest(login.RefreshToken), CancellationToken.None);

        await Assert.ThrowsAsync<UnauthorizedAppException>(() =>
            sut.RefreshAsync(new RefreshTokenRequest(login.RefreshToken), CancellationToken.None));

        Assert.Contains("auth.refresh.reuse_detected", audit.Actions);
        Assert.Equal(0, await db.RefreshTokens.CountAsync(t => t.RevokedAt == null));
    }

    private static AuthService CreateSut(
        out AppDbContext db,
        out User user,
        out FakeAuditService audit)
    {
        db = TestDb.Create();
        audit = new FakeAuditService();
        var hasher = new TestPasswordHasher();
        var clock = new FixedDateTimeProvider();
        var currentUser = new FakeCurrentUserService();

        var permission = new Permission
        {
            Code = Permissions.Clients.Read,
            Name = "Consultar clientes",
            Module = "Clientes",
        };

        var role = new Role
        {
            Name = "Comercial",
            NormalizedName = "comercial",
            IsSystemRole = true,
        };

        role.RolePermissions.Add(new RolePermission { Permission = permission });

        user = new User
        {
            Email = "user@choy.local",
            NormalizedEmail = "user@choy.local",
            PasswordHash = hasher.Hash("Password123"),
            IsActive = true,
        };

        user.UserRoles.Add(new UserRole { Role = role });
        db.Users.Add(user);
        db.SaveChanges();

        return new AuthService(
            db,
            hasher,
            new JwtTokenService(TestDb.JwtOptions, clock),
            audit,
            currentUser,
            clock,
            TestDb.JwtOptions);
    }
}
