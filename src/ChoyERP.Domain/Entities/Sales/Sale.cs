using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Installations;

namespace ChoyERP.Domain.Entities.Sales;

/// <summary>
/// Venta de equipos a un cliente. Puede asociarse a un proyecto y derivar en
/// una instalación. El stock solo se descuenta al confirmar la venta.
/// </summary>
public class Sale : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public SaleStatus Status { get; set; } = SaleStatus.Draft;

    public string Currency { get; set; } = "PEN";

    public decimal TaxRate { get; set; } = 0.18m;

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public DateOnly SaleDate { get; set; }

    public DateOnly? ExpectedDeliveryDate { get; set; }

    public DateOnly? DeliveredDate { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public string? Notes { get; set; }

    public ICollection<SaleItem> Items { get; set; } = [];

    public Installation? Installation { get; set; }

    public bool IsEditable => Status == SaleStatus.Draft;

    /// <summary>Recalcula los importes a partir de las líneas y la tasa de impuesto.</summary>
    public void RecalculateTotals()
    {
        Subtotal = decimal.Round(
            Items.Sum(item => item.LineTotal),
            2,
            MidpointRounding.AwayFromZero);

        TaxAmount = decimal.Round(
            Subtotal * TaxRate,
            2,
            MidpointRounding.AwayFromZero);

        Total = Subtotal + TaxAmount;
    }
}
