using ChoyERP.Domain.Common;

namespace ChoyERP.Domain.Entities.Contracting;

/// <summary>Línea de detalle de una cotización.</summary>
public class QuoteItem : BaseEntity
{
    public Guid QuoteId { get; set; }

    public Quote Quote { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public int SortOrder { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => decimal.Round(
        Quantity * UnitPrice,
        2,
        MidpointRounding.AwayFromZero);
}
