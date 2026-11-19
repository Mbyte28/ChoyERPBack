using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Sales;

namespace ChoyERP.Domain.Entities.Inventory;

/// <summary>
/// Movimiento de inventario (entrada, salida o ajuste). Conserva la fotografía
/// de la existencia antes y después para construir el kardex del artículo.
/// </summary>
public class InventoryMovement : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid ItemId { get; set; }

    public InventoryItem Item { get; set; } = null!;

    public InventoryMovementType Type { get; set; }

    public DateOnly MovementDate { get; set; }

    /// <summary>
    /// Magnitud registrada por el usuario. En una entrada o salida es la cantidad
    /// movida; en un ajuste es la existencia contada resultante.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>Variación firmada de la existencia (positiva en entradas).</summary>
    public decimal QuantityDelta { get; set; }

    public decimal StockBefore { get; set; }

    public decimal StockAfter { get; set; }

    /// <summary>Costo unitario aplicado al movimiento.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// Costo del movimiento con signo: positivo en entradas, negativo en salidas
    /// y ajustes por faltante.
    /// </summary>
    public decimal TotalCost { get; set; }

    public string? Reason { get; set; }

    /// <summary>Documento de respaldo: guía, factura, orden de compra o de trabajo.</summary>
    public string? Reference { get; set; }

    /// <summary>Proyecto al que se imputa el movimiento, normalmente una salida.</summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    /// <summary>Venta que originó la salida de equipos, si corresponde.</summary>
    public Guid? SaleId { get; set; }

    public Sale? Sale { get; set; }

    public string? Notes { get; set; }
}
