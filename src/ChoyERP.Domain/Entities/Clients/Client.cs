using ChoyERP.Domain.Common;

namespace ChoyERP.Domain.Entities.Clients;

/// <summary>
/// Cliente de CHOY. En la primera versión no tiene acceso al sistema.
/// </summary>
public class Client : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? LegalName { get; set; }

    public string? TaxId { get; set; }

    public string? ContactName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ClientAddress> Addresses { get; set; } = [];
}
