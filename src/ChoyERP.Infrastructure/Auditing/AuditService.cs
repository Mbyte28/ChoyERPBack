using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Domain.Entities.Auditing;
using ChoyERP.Infrastructure.Persistence;

namespace ChoyERP.Infrastructure.Auditing;

public sealed class AuditService(
    AppDbContext db,
    ICurrentUserService currentUser,
    IDateTimeProvider clock) : IAuditService
{
    public async Task LogAsync(
        string action,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        Guid? userId = null,
        string? userEmail = null,
        CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            OccurredAt = clock.UtcNow,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = Truncate(details, 4000),
            UserId = userId ?? currentUser.UserId,
            UserEmail = userEmail ?? currentUser.Email,
            IpAddress = currentUser.IpAddress,
            UserAgent = Truncate(currentUser.UserAgent, 500),
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
