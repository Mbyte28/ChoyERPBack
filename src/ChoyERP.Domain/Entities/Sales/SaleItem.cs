using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Inventory;

namespace ChoyERP.Domain.Entities.Sales;

/// <summary>
/// Línea de una venta. Conserva la descripción y la unidad del artículo al
/// momento de la venta para que los cambios posteriores del catálogo no alteren
/// el historial.
/// </summary>
public class SaleItem : BaseEntity
{
    public Guid SaleId { get; set; }

    public Sale Sale { get; set; } = null!;

    public Guid InventoryItemId { get; set; }

    public InventoryItem InventoryItem { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>Orden de la línea dentro de la venta.</summary>
    public int SortOrder { get; set; }

    public decimal LineTotal =>
        decimal.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
}
