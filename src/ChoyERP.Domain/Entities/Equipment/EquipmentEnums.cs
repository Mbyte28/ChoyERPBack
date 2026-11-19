namespace ChoyERP.Domain.Entities.Equipment;

/// <summary>Estado operativo de un equipo instalado en el cliente.</summary>
public enum EquipmentStatus
{
    /// <summary>Equipo en funcionamiento normal.</summary>
    Operational = 0,

    /// <summary>Equipo detenido por mantenimiento preventivo o correctivo.</summary>
    UnderMaintenance = 1,

    /// <summary>Equipo fuera de servicio a la espera de reparación o repuesto.</summary>
    OutOfService = 2,

    /// <summary>Equipo dado de baja definitiva. Es un estado terminal.</summary>
    Retired = 3,
}
