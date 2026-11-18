using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Workers;

namespace ChoyERP.Domain.Entities.Contracting;

/// <summary>
/// Solicitud inicial de un cliente para un proyecto de refrigeración.
/// Es el punto de entrada del módulo de contrataciones.
/// </summary>
public class ProjectRequest : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    /// <summary>Dirección o instalación donde se ejecutará el proyecto.</summary>
    public Guid? ClientAddressId { get; set; }

    public ClientAddress? ClientAddress { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectType ProjectType { get; set; } = ProjectType.ColdRoom;

    public RequestPriority Priority { get; set; } = RequestPriority.Normal;

    public ProjectRequestStatus Status { get; set; } = ProjectRequestStatus.Received;

    public DateOnly RequestedAt { get; set; }

    public Guid? ResponsibleWorkerId { get; set; }

    public Worker? ResponsibleWorker { get; set; }

    public string? Notes { get; set; }

    public ICollection<Quote> Quotes { get; set; } = [];
}
