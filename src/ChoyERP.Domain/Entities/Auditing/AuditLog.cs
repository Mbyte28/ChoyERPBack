namespace ChoyERP.Domain.Entities.Auditing;

/// <summary>
/// Registro de operaciones relevantes (accesos, cambios de usuarios y clientes).
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset OccurredAt { get; set; }

    public Guid? UserId { get; set; }

    public string? UserEmail { get; set; }

    /// <summary>Código de acción, por ejemplo <c>auth.login.succeeded</c>.</summary>
    public string Action { get; set; } = string.Empty;

    public string? EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Details { get; set; }
}
