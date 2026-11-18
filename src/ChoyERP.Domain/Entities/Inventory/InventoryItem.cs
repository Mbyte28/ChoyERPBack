using ChoyERP.Domain.Common;

namespace ChoyERP.Domain.Entities.Inventory;

/// <summary>
/// Artículo del almacén: repuesto, componente, equipo, insumo o herramienta.
/// La existencia solo cambia mediante movimientos de inventario.
/// </summary>
public class InventoryItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public InventoryCategory Category { get; set; } = InventoryCategory.Spare;

    public InventoryUnit Unit { get; set; } = InventoryUnit.Unit;

    public string? Brand { get; set; }

    public string? Model { get; set; }

    /// <summary>Ubicación física dentro del almacén.</summary>
    public string? Location { get; set; }

    /// <summary>Existencia actual, mantenida por los movimientos.</summary>
    public decimal QuantityOnHand { get; set; }

    /// <summary>Nivel mínimo antes de generar una alerta de reposición.</summary>
    public decimal MinimumStock { get; set; }

    /// <summary>Costo promedio ponderado por unidad.</summary>
    public decimal AverageUnitCost { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public ICollection<InventoryMovement> Movements { get; set; } = [];

    public bool IsBelowMinimum => QuantityOnHand < MinimumStock;

    public decimal TotalValue => QuantityOnHand * AverageUnitCost;
}
