using ChoyERP.Application.Features.Equipment.Dtos;
using ChoyERP.Domain.Entities.Equipment;

namespace ChoyERP.Application.Features.Equipment;

internal static class EquipmentMappings
{
    public static EquipmentDto ToDto(this InstalledEquipment equipment, DateOnly today) =>
        new(
            equipment.Id,
            equipment.Code,
            equipment.ClientId,
            equipment.Client.Name,
            equipment.InstallationId,
            equipment.Installation.Code,
            equipment.InventoryItemId,
            equipment.InventoryItem.Code,
            equipment.InventoryItem.Name,
            equipment.Name,
            equipment.SerialNumber,
            equipment.Brand,
            equipment.Model,
            equipment.Location,
            equipment.InstalledDate,
            equipment.WarrantyEndDate,
            equipment.WarrantyEndDate is not null && equipment.WarrantyEndDate >= today,
            equipment.Status,
            equipment.Notes,
            equipment.CreatedAt,
            equipment.UpdatedAt);

    public static EquipmentStatusChangeDto ToDto(this EquipmentStatusChange change) =>
        new(
            change.Id,
            change.EquipmentId,
            change.FromStatus,
            change.ToStatus,
            change.ChangedDate,
            change.Reason,
            change.CreatedAt);
}
