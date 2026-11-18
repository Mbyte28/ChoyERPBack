using ChoyERP.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChoyERP.Infrastructure.Persistence.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items", table =>
        {
            table.HasCheckConstraint("ck_inventory_items_quantity_on_hand", "quantity_on_hand >= 0");
            table.HasCheckConstraint("ck_inventory_items_minimum_stock", "minimum_stock >= 0");
            table.HasCheckConstraint("ck_inventory_items_average_unit_cost", "average_unit_cost >= 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code).HasMaxLength(30).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Description).HasMaxLength(2000);
        builder.Property(i => i.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Unit).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.Brand).HasMaxLength(100);
        builder.Property(i => i.Model).HasMaxLength(100);
        builder.Property(i => i.Location).HasMaxLength(150);
        builder.Property(i => i.QuantityOnHand).HasPrecision(18, 3);
        builder.Property(i => i.MinimumStock).HasPrecision(18, 3);
        builder.Property(i => i.AverageUnitCost).HasPrecision(18, 4);
        builder.Property(i => i.Notes).HasMaxLength(2000);

        builder.HasIndex(i => i.Code).IsUnique();
        builder.HasIndex(i => i.Name);
        builder.HasIndex(i => i.Category);
        builder.HasIndex(i => i.IsActive);

        builder.Ignore(i => i.IsBelowMinimum);
        builder.Ignore(i => i.TotalValue);

        builder.HasMany(i => i.Movements)
            .WithOne(m => m.Item)
            .HasForeignKey(m => m.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements", table =>
        {
            table.HasCheckConstraint("ck_inventory_movements_quantity", "quantity >= 0");
            table.HasCheckConstraint("ck_inventory_movements_stock_before", "stock_before >= 0");
            table.HasCheckConstraint("ck_inventory_movements_stock_after", "stock_after >= 0");
            table.HasCheckConstraint("ck_inventory_movements_unit_cost", "unit_cost >= 0");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Code).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(m => m.Quantity).HasPrecision(18, 3);
        builder.Property(m => m.QuantityDelta).HasPrecision(18, 3);
        builder.Property(m => m.StockBefore).HasPrecision(18, 3);
        builder.Property(m => m.StockAfter).HasPrecision(18, 3);
        builder.Property(m => m.UnitCost).HasPrecision(18, 4);
        builder.Property(m => m.TotalCost).HasPrecision(18, 2);
        builder.Property(m => m.Reason).HasMaxLength(500);
        builder.Property(m => m.Reference).HasMaxLength(100);
        builder.Property(m => m.Notes).HasMaxLength(2000);

        builder.HasIndex(m => m.Code).IsUnique();
        builder.HasIndex(m => m.ItemId);
        builder.HasIndex(m => m.Type);
        builder.HasIndex(m => m.MovementDate);
        builder.HasIndex(m => new { m.ItemId, m.MovementDate });
        builder.HasIndex(m => m.ProjectId);

        builder.HasOne(m => m.Project)
            .WithMany()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
