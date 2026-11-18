using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Auditing;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Inventory;
using ChoyERP.Domain.Entities.Users;
using ChoyERP.Domain.Entities.Workers;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Worker> Workers => Set<Worker>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<ClientAddress> ClientAddresses => Set<ClientAddress>();

    public DbSet<ProjectRequest> ProjectRequests => Set<ProjectRequest>();

    public DbSet<Quote> Quotes => Set<Quote>();

    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
