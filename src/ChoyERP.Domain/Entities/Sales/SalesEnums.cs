namespace ChoyERP.Domain.Entities.Sales;

/// <summary>Estado del ciclo de vida de una venta de equipos.</summary>
public enum SaleStatus
{
    /// <summary>Venta en preparación; las líneas son editables y no afecta el stock.</summary>
    Draft = 0,

    /// <summary>Venta confirmada; el stock se descontó y las líneas quedaron congeladas.</summary>
    Confirmed = 1,

    /// <summary>Equipos entregados al cliente.</summary>
    Delivered = 2,

    /// <summary>Venta cerrada, normalmente tras completar la instalación.</summary>
    Completed = 3,

    /// <summary>Venta anulada antes de confirmarse.</summary>
    Cancelled = 4,
}
