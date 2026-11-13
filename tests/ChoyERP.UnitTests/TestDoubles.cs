using System.Security.Cryptography;
using System.Text;
using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChoyERP.UnitTests;

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public string? IpAddress { get; set; } = "127.0.0.1";

    public string? UserAgent { get; set; } = "xunit";

    public bool IsAuthenticated => UserId is not null;

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedAppException("La sesión no es válida.");
}

internal sealed class FakeAuditService : IAuditService
{
    public List<string> Actions { get; } = [];

    public Task LogAsync(
        string action,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        Guid? userId = null,
        string? userEmail = null,
        CancellationToken cancellationToken = default)
    {
        Actions.Add(action);
        return Task.CompletedTask;
    }
}

/// <summary>Hasher rápido y determinista para pruebas; no usar en producción.</summary>
internal sealed class TestPasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        "test:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    public bool Verify(string password, string passwordHash) => Hash(password) == passwordHash;
}

internal sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);
}

internal static class TestDb
{
    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"choyerp-tests-{Guid.NewGuid()}")
            .Options);

    public static IOptions<JwtOptions> JwtOptions { get; } = Options.Create(new JwtOptions
    {
        Issuer = "ChoyERP.Tests",
        Audience = "ChoyERP.Tests.Client",
        SigningKey = new string('k', 64),
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7,
    });
}
