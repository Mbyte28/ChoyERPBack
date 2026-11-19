using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Inventory;

namespace ChoyERP.Domain.Entities.Equipment;

/// <summary>
/// Equipo entregado e instalado en las instalaciones de un cliente. Conserva la
/// identidad física (serie, marca y modelo) y su estado operativo para el
/// seguimiento posterior. Solo puede registrarse sobre una instalación completada.
/// </summary>
public class InstalledEquipment : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public Guid ClientId { get; set; }

    public Client Client { get; set; } = null!;

    /// <summary>Instalación en la que quedó montado el equipo.</summary>
    public Guid InstallationId { get; set; }

    public Installation Installation { get; set; } = null!;

    /// <summary>Artículo de almacén que identifica el modelo del equipo.</summary>
    public Guid InventoryItemId { get; set; }

    public InventoryItem InventoryItem { get; set; } = null!;

    /// <summary>Nombre visible del equipo; por defecto el del artículo.</summary>
    public string Name { get; set; } = string.Empty;

    public string? SerialNumber { get; set; }

    public string? Brand { get; set; }

    public string? Model { get; set; }

    /// <summary>Ubicación física dentro de las instalaciones del cliente.</summary>
    public string? Location { get; set; }

    public DateOnly InstalledDate { get; set; }

    public DateOnly? WarrantyEndDate { get; set; }

    public EquipmentStatus Status { get; set; } = EquipmentStatus.Operational;

    public string? Notes { get; set; }

    public ICollection<EquipmentStatusChange> StatusHistory { get; set; } = [];

    public bool IsRetired => Status == EquipmentStatus.Retired;
}
