using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Features.Users;
using ChoyERP.Application.Features.Users.Dtos;
using ChoyERP.Domain.Entities.Users;
using ChoyERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.UnitTests.Features;

public sealed class UserServiceTests
{
    [Fact]
    public async Task Create_WithDuplicateEmail_ThrowsConflict()
    {
        var db = TestDb.Create();
        var role = AddRole(db);
        var currentUser = new FakeCurrentUserService();
        var service = CreateService(db, currentUser);

        var existing = new User
        {
            Email = "duplicado@choy.local",
            NormalizedEmail = "duplicado@choy.local",
            PasswordHash = "test:x",
        };

        db.Users.Add(existing);
        await db.SaveChangesAsync();

        var request = new CreateUserRequest(
            "Duplicado@choy.local",
            "Password123",
            null,
            [role.Id]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task SetStatus_CannotDeactivateOwnAccount()
    {
        var db = TestDb.Create();
        var user = AddUser(db, "admin@choy.local");
        var currentUser = new FakeCurrentUserService { UserId = user.Id, Email = user.Email };
        var service = CreateService(db, currentUser);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.SetStatusAsync(user.Id, false, CancellationToken.None));
    }

    [Fact]
    public async Task SetStatus_DeactivatingUser_RevokesActiveSessions()
    {
        var db = TestDb.Create();
        var user = AddUser(db, "tecnico@choy.local");
        var admin = AddUser(db, "admin@choy.local");

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = new string('a', 64),
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
        });

        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { UserId = admin.Id, Email = admin.Email };
        var service = CreateService(db, currentUser);

        var result = await service.SetStatusAsync(user.Id, false, CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.Equal(0, await db.RefreshTokens.CountAsync(t => t.RevokedAt == null));
    }

    private static UserService CreateService(AppDbContext db, FakeCurrentUserService currentUser) =>
        new(
            db,
            new TestPasswordHasher(),
            new FakeAuditService(),
            currentUser,
            new FixedDateTimeProvider());

    private static Role AddRole(AppDbContext db)
    {
        var role = new Role
        {
            Name = "Técnico",
            NormalizedName = "técnico",
            IsSystemRole = true,
        };

        db.Roles.Add(role);
        db.SaveChanges();
        return role;
    }

    private static User AddUser(AppDbContext db, string email)
    {
        var user = new User
        {
            Email = email,
            NormalizedEmail = email.ToLowerInvariant(),
            PasswordHash = "test:x",
        };

        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }
}
