using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Sales;
using ChoyERP.Domain.Entities.Workers;

namespace ChoyERP.Domain.Entities.Installations;

/// <summary>
/// Instalación asociada a una venta de equipos o a un proyecto. Es la entidad
/// que agrupará los equipos instalados y su historial técnico.
/// </summary>
public class Installation : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    /// <summary>Venta que origina la instalación, si corresponde.</summary>
    public Guid? SaleId { get; set; }

    public Sale? Sale { get; set; }

    /// <summary>Proyecto que origina la instalación, si corresponde.</summary>
    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid? ClientAddressId { get; set; }

    public ClientAddress? ClientAddress { get; set; }

    /// <summary>Ubicación textual congelada en el momento del registro.</summary>
    public string? Location { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public InstallationStatus Status { get; set; } = InstallationStatus.Scheduled;

    public DateOnly? ScheduledDate { get; set; }

    public DateOnly? StartedDate { get; set; }

    public DateOnly? CompletedDate { get; set; }

    public Guid? TechnicianWorkerId { get; set; }

    public Worker? Technician { get; set; }

    public string? CancellationReason { get; set; }

    public string? Notes { get; set; }
}
