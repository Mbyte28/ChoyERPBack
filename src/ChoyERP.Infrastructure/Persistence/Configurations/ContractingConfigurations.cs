using ChoyERP.Domain.Entities.Contracting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChoyERP.Infrastructure.Persistence.Configurations;

public sealed class ProjectRequestConfiguration : IEntityTypeConfiguration<ProjectRequest>
{
    public void Configure(EntityTypeBuilder<ProjectRequest> builder)
    {
        builder.ToTable("project_requests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Code).HasMaxLength(30).IsRequired();
        builder.Property(r => r.Title).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(2000);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.ProjectType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(r => r.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(r => r.Code).IsUnique();
        builder.HasIndex(r => r.ClientId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.RequestedAt);
        builder.HasIndex(r => r.ResponsibleWorkerId);

        builder.HasOne(r => r.Client)
            .WithMany()
            .HasForeignKey(r => r.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ClientAddress)
            .WithMany()
            .HasForeignKey(r => r.ClientAddressId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.ResponsibleWorker)
            .WithMany()
            .HasForeignKey(r => r.ResponsibleWorkerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("quotes");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Code).HasMaxLength(30).IsRequired();
        builder.Property(q => q.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(q => q.Currency).HasMaxLength(3).IsRequired();
        builder.Property(q => q.TaxRate).HasPrecision(5, 4);
        builder.Property(q => q.Subtotal).HasPrecision(18, 2);
        builder.Property(q => q.TaxAmount).HasPrecision(18, 2);
        builder.Property(q => q.Total).HasPrecision(18, 2);
        builder.Property(q => q.Notes).HasMaxLength(2000);
        builder.Property(q => q.DecisionNotes).HasMaxLength(1000);

        builder.HasIndex(q => q.Code).IsUnique();
        builder.HasIndex(q => new { q.ProjectRequestId, q.Version }).IsUnique();
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.ValidUntil);

        builder.HasOne(q => q.ProjectRequest)
            .WithMany(r => r.Quotes)
            .HasForeignKey(q => q.ProjectRequestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Items)
            .WithOne(i => i.Quote)
            .HasForeignKey(i => i.QuoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class QuoteItemConfiguration : IEntityTypeConfiguration<QuoteItem>
{
    public void Configure(EntityTypeBuilder<QuoteItem> builder)
    {
        builder.ToTable("quote_items", table =>
            table.HasCheckConstraint("ck_quote_items_quantity", "quantity > 0"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Description).HasMaxLength(500).IsRequired();
        builder.Property(i => i.Unit).HasMaxLength(20);
        builder.Property(i => i.SortOrder).IsRequired();
        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);

        builder.HasIndex(i => i.QuoteId);
        builder.Ignore(i => i.LineTotal);
    }
}

public sealed class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).HasMaxLength(30).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.Currency).HasMaxLength(3).IsRequired();
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.QuoteId).IsUnique();
        builder.HasIndex(c => c.ClientId);
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.Quote)
            .WithOne(q => q.Contract)
            .HasForeignKey<Contract>(c => c.QuoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Client)
            .WithMany()
            .HasForeignKey(c => c.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Project)
            .WithOne(p => p.Contract)
            .HasForeignKey<Project>(p => p.ContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects", table =>
            table.HasCheckConstraint("ck_projects_progress", "progress_percent BETWEEN 0 AND 100"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(30).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Location).HasMaxLength(300);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.ContractId).IsUnique();
        builder.HasIndex(p => p.ClientId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.ResponsibleWorkerId);

        builder.HasOne(p => p.Client)
            .WithMany()
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ClientAddress)
            .WithMany()
            .HasForeignKey(p => p.ClientAddressId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.ResponsibleWorker)
            .WithMany()
            .HasForeignKey(p => p.ResponsibleWorkerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
