using ChoyERP.Domain.Common;

namespace ChoyERP.Domain.Entities.Equipment;

/// <summary>
/// Cambio de estado de un equipo instalado. <see cref="FromStatus"/> es nulo en
/// el registro inicial y queda congelado para conservar el historial.
/// </summary>
public class EquipmentStatusChange : BaseEntity
{
    public Guid EquipmentId { get; set; }

    public InstalledEquipment Equipment { get; set; } = null!;

    public EquipmentStatus? FromStatus { get; set; }

    public EquipmentStatus ToStatus { get; set; }

    public DateOnly ChangedDate { get; set; }

    public string Reason { get; set; } = string.Empty;
}
