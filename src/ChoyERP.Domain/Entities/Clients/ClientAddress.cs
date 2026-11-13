using ChoyERP.Domain.Common;

namespace ChoyERP.Domain.Entities.Clients;

/// <summary>
/// Dirección asociada a un cliente (sede, planta o punto de contacto).
/// </summary>
public class ClientAddress : BaseEntity
{
    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public string Label { get; set; } = string.Empty;

    public string Line1 { get; set; } = string.Empty;

    public string? Line2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? Country { get; set; }

    public string? PostalCode { get; set; }

    public bool IsPrimary { get; set; }
}
