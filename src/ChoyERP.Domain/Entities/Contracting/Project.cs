using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Workers;

namespace ChoyERP.Domain.Entities.Contracting;

/// <summary>
/// Proyecto de ejecución derivado de un contrato. Es la entidad que
/// referencian ingeniería, instalaciones, equipos y mantenimiento.
/// </summary>
public class Project : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid ContractId { get; set; }

    public Contract Contract { get; set; } = null!;

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    public Guid? ClientAddressId { get; set; }

    public ClientAddress? ClientAddress { get; set; }

    /// <summary>Ubicación textual al momento de crear el proyecto.</summary>
    public string? Location { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    public int ProgressPercent { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? ExpectedEndDate { get; set; }

    public DateOnly? ActualEndDate { get; set; }

    public Guid? ResponsibleWorkerId { get; set; }

    public Worker? ResponsibleWorker { get; set; }

    public string? Notes { get; set; }
}
