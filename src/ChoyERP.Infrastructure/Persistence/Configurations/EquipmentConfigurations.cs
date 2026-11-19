using ChoyERP.Domain.Entities.Equipment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChoyERP.Infrastructure.Persistence.Configurations;

public sealed class InstalledEquipmentConfiguration
    : IEntityTypeConfiguration<InstalledEquipment>
{
    public void Configure(EntityTypeBuilder<InstalledEquipment> builder)
    {
        builder.ToTable("installed_equipment", table =>
            table.HasCheckConstraint(
                "ck_installed_equipment_warranty",
                "warranty_end_date IS NULL OR warranty_end_date >= installed_date"));

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).HasMaxLength(30).IsRequired();
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.SerialNumber).HasMaxLength(100);
        builder.Property(e => e.Brand).HasMaxLength(100);
        builder.Property(e => e.Model).HasMaxLength(100);
        builder.Property(e => e.Location).HasMaxLength(300);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.ClientId);
        builder.HasIndex(e => e.InstallationId);
        builder.HasIndex(e => e.InventoryItemId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.InstalledDate);
        builder.HasIndex(e => e.WarrantyEndDate);
        builder.HasIndex(e => new { e.InventoryItemId, e.SerialNumber }).IsUnique();

        builder.Ignore(e => e.IsRetired);

        builder.HasOne(e => e.Client)
            .WithMany()
            .HasForeignKey(e => e.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Installation)
            .WithMany()
            .HasForeignKey(e => e.InstallationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.InventoryItem)
            .WithMany()
            .HasForeignKey(e => e.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.StatusHistory)
            .WithOne(change => change.Equipment)
            .HasForeignKey(change => change.EquipmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EquipmentStatusChangeConfiguration
    : IEntityTypeConfiguration<EquipmentStatusChange>
{
    public void Configure(EntityTypeBuilder<EquipmentStatusChange> builder)
    {
        builder.ToTable("equipment_status_changes", table =>
            table.HasCheckConstraint(
                "ck_equipment_status_changes_transition",
                "from_status IS NULL OR from_status <> to_status"));

        builder.HasKey(change => change.Id);

        builder.Property(change => change.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(change => change.ToStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(change => change.Reason).HasMaxLength(500).IsRequired();

        builder.HasIndex(change => change.EquipmentId);
        builder.HasIndex(change => change.ChangedDate);
    }
}
