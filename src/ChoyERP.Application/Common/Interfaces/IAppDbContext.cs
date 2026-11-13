using ChoyERP.Domain.Entities.Auditing;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Users;
using ChoyERP.Domain.Entities.Workers;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Common.Interfaces;

/// <summary>
/// Acceso a la persistencia visto desde la capa de aplicación.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }

    DbSet<Role> Roles { get; }

    DbSet<Permission> Permissions { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Worker> Workers { get; }

    DbSet<Client> Clients { get; }

    DbSet<ClientAddress> ClientAddresses { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
