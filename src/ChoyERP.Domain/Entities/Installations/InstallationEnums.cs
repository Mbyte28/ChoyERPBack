namespace ChoyERP.Domain.Entities.Installations;

/// <summary>Estado del ciclo de vida de una instalación.</summary>
public enum InstallationStatus
{
    /// <summary>Instalación programada, aún no iniciada.</summary>
    Scheduled = 0,

    /// <summary>Trabajos de instalación en curso.</summary>
    InProgress = 1,

    /// <summary>Instalación terminada.</summary>
    Completed = 2,

    /// <summary>Instalación anulada antes de completarse.</summary>
    Cancelled = 3,
}
