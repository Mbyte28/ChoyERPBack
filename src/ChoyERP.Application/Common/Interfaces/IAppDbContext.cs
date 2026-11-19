using ChoyERP.Domain.Entities.Auditing;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Equipment;
using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Inventory;
using ChoyERP.Domain.Entities.Sales;
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

    DbSet<ProjectRequest> ProjectRequests { get; }

    DbSet<Quote> Quotes { get; }

    DbSet<QuoteItem> QuoteItems { get; }

    DbSet<Contract> Contracts { get; }

    DbSet<Project> Projects { get; }

    DbSet<InventoryItem> InventoryItems { get; }

    DbSet<InventoryMovement> InventoryMovements { get; }

    DbSet<Sale> Sales { get; }

    DbSet<SaleItem> SaleItems { get; }

    DbSet<Installation> Installations { get; }

    DbSet<InstalledEquipment> InstalledEquipment { get; }

    DbSet<EquipmentStatusChange> EquipmentStatusChanges { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
