using ChoyERP.Application.Features.Inventory.Dtos;
using ChoyERP.Domain.Entities.Inventory;

namespace ChoyERP.Application.Features.Inventory;

internal static class InventoryMappings
{
    public static InventoryItemDto ToDto(this InventoryItem item) =>
        new(
            item.Id,
            item.Code,
            item.Name,
            item.Description,
            item.Category,
            item.Unit,
            item.Brand,
            item.Model,
            item.Location,
            item.QuantityOnHand,
            item.MinimumStock,
            item.AverageUnitCost,
            item.TotalValue,
            item.IsBelowMinimum,
            item.IsActive,
            item.Notes,
            item.CreatedAt,
            item.UpdatedAt);

    public static InventoryMovementDto ToDto(this InventoryMovement movement) =>
        new(
            movement.Id,
            movement.Code,
            movement.ItemId,
            movement.Item.Code,
            movement.Item.Name,
            movement.Type,
            movement.MovementDate,
            movement.Quantity,
            movement.QuantityDelta,
            movement.StockBefore,
            movement.StockAfter,
            movement.UnitCost,
            movement.TotalCost,
            movement.Reason,
            movement.Reference,
            movement.ProjectId,
            movement.Project?.Code,
            movement.Project?.Name,
            movement.Notes,
            movement.CreatedAt,
            movement.UpdatedAt);
}
