namespace ChoyERP.Application.Common.Interfaces;

/// <summary>
/// Registra operaciones relevantes sin exponer datos sensibles.
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        string action,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        Guid? userId = null,
        string? userEmail = null,
        CancellationToken cancellationToken = default);
}
