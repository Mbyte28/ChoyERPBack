using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Clients;

namespace ChoyERP.Domain.Entities.Contracting;

/// <summary>
/// Contrato u orden aceptada por el cliente a partir de una cotización.
/// </summary>
public class Contract : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid QuoteId { get; set; }

    public Quote Quote { get; set; } = null!;

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    /// <summary>Importe acordado, tomado de la cotización aceptada.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "PEN";

    public DateOnly? SignedAt { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Notes { get; set; }

    public Project? Project { get; set; }
}
