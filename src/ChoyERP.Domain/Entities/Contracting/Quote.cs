using ChoyERP.Domain.Common;

namespace ChoyERP.Domain.Entities.Contracting;

/// <summary>
/// Cotización comercial de una solicitud. Cada versión es una cotización
/// independiente y solo una puede quedar aceptada.
/// </summary>
public class Quote : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid ProjectRequestId { get; set; }

    public ProjectRequest ProjectRequest { get; set; } = null!;

    public int Version { get; set; } = 1;

    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;

    public string Currency { get; set; } = "PEN";

    public decimal TaxRate { get; set; } = 0.18m;

    public decimal Subtotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal Total { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public string? Notes { get; set; }

    /// <summary>Motivo registrado al aceptar o rechazar la cotización.</summary>
    public string? DecisionNotes { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }

    public ICollection<QuoteItem> Items { get; set; } = [];

    public Contract? Contract { get; set; }

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
