using ChoyERP.Application.Common.Models;
using ChoyERP.Domain.Entities.Inventory;

namespace ChoyERP.Application.Features.Inventory.Dtos;

public sealed record InventoryItemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    InventoryCategory Category,
    InventoryUnit Unit,
    string? Brand,
    string? Model,
    string? Location,
    decimal QuantityOnHand,
    decimal MinimumStock,
    decimal AverageUnitCost,
    decimal TotalValue,
    bool IsBelowMinimum,
    bool IsActive,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record InventoryMovementDto(
    Guid Id,
    string Code,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    InventoryMovementType Type,
    DateOnly MovementDate,
    decimal Quantity,
    decimal QuantityDelta,
    decimal StockBefore,
    decimal StockAfter,
    decimal UnitCost,
    decimal TotalCost,
    string? Reason,
    string? Reference,
    Guid? ProjectId,
    string? ProjectCode,
    string? ProjectName,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateInventoryItemRequest(
    string? Code,
    string Name,
    string? Description,
    InventoryCategory Category,
    InventoryUnit Unit,
    string? Brand,
    string? Model,
    string? Location,
    decimal MinimumStock,
    decimal UnitCost,
    string? Notes);

public sealed record UpdateInventoryItemRequest(
    string Name,
    string? Description,
    InventoryCategory Category,
    InventoryUnit Unit,
    string? Brand,
    string? Model,
    string? Location,
    decimal MinimumStock,
    string? Notes);

public sealed record CreateInventoryMovementRequest(
    Guid ItemId,
    InventoryMovementType Type,
    decimal Quantity,
    decimal? UnitCost,
    DateOnly? MovementDate,
    string? Reason,
    string? Reference,
    Guid? ProjectId,
    string? Notes);

public sealed class InventoryItemQuery : PaginationQuery
{
    public string? Search { get; set; }

    public InventoryCategory? Category { get; set; }

    public InventoryUnit? Unit { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>Si es verdadero, solo devuelve artículos por debajo del stock mínimo.</summary>
    public bool? BelowMinimum { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}

public sealed class InventoryMovementQuery : PaginationQuery
{
    public string? Search { get; set; }

    public Guid? ItemId { get; set; }

    public InventoryMovementType? Type { get; set; }

    public Guid? ProjectId { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}
