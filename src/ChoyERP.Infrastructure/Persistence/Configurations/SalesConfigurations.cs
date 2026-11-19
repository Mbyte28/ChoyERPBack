using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChoyERP.Infrastructure.Persistence.Configurations;

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales", table =>
        {
            table.HasCheckConstraint(
                "ck_sales_tax_rate",
                "tax_rate >= 0 AND tax_rate <= 1");
            table.HasCheckConstraint("ck_sales_subtotal", "subtotal >= 0");
            table.HasCheckConstraint("ck_sales_tax_amount", "tax_amount >= 0");
            table.HasCheckConstraint("ck_sales_total", "total >= 0");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Code).HasMaxLength(30).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Currency).HasMaxLength(3).IsRequired();
        builder.Property(s => s.TaxRate).HasPrecision(5, 4);
        builder.Property(s => s.Subtotal).HasPrecision(18, 2);
        builder.Property(s => s.TaxAmount).HasPrecision(18, 2);
        builder.Property(s => s.Total).HasPrecision(18, 2);
        builder.Property(s => s.CancellationReason).HasMaxLength(500);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.ClientId);
        builder.HasIndex(s => s.ProjectId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.SaleDate);

        builder.Ignore(s => s.IsEditable);

        builder.HasOne(s => s.Client)
            .WithMany()
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Project)
            .WithMany()
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.Items)
            .WithOne(item => item.Sale)
            .HasForeignKey(item => item.SaleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("sale_items", table =>
        {
            table.HasCheckConstraint("ck_sale_items_quantity", "quantity > 0");
            table.HasCheckConstraint("ck_sale_items_unit_price", "unit_price >= 0");
        });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Description).HasMaxLength(500).IsRequired();
        builder.Property(item => item.Unit).HasMaxLength(20);
        builder.Property(item => item.Quantity).HasPrecision(18, 3);
        builder.Property(item => item.UnitPrice).HasPrecision(18, 2);
        builder.Property(item => item.SortOrder).IsRequired();

        builder.HasIndex(item => item.SaleId);
        builder.HasIndex(item => item.InventoryItemId);

        builder.Ignore(item => item.LineTotal);

        builder.HasOne(item => item.InventoryItem)
            .WithMany()
            .HasForeignKey(item => item.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InstallationConfiguration : IEntityTypeConfiguration<Installation>
{
    public void Configure(EntityTypeBuilder<Installation> builder)
    {
        builder.ToTable("installations", table =>
            table.HasCheckConstraint(
                "ck_installations_origin",
                "(sale_id IS NULL) <> (project_id IS NULL)"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(2000);
        builder.Property(i => i.Location).HasMaxLength(300);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.CancellationReason).HasMaxLength(500);
        builder.Property(i => i.Notes).HasMaxLength(2000);

        builder.HasIndex(i => i.Code).IsUnique();
        builder.HasIndex(i => i.ClientId);
        builder.HasIndex(i => i.SaleId).IsUnique();
        builder.HasIndex(i => i.ProjectId).IsUnique();
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.ScheduledDate);
        builder.HasIndex(i => i.TechnicianWorkerId);

        builder.HasOne(i => i.Client)
            .WithMany()
            .HasForeignKey(i => i.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Sale)
            .WithOne(s => s.Installation)
            .HasForeignKey<Installation>(i => i.SaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Project)
            .WithMany()
            .HasForeignKey(i => i.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ClientAddress)
            .WithMany()
            .HasForeignKey(i => i.ClientAddressId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(i => i.Technician)
            .WithMany()
            .HasForeignKey(i => i.TechnicianWorkerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
