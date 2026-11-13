namespace ChoyERP.Application.Common.Interfaces;

/// <summary>
/// Identidad de la petición actual, resuelta desde el token autenticado.
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Email { get; }

    string? IpAddress { get; }

    string? UserAgent { get; }

    bool IsAuthenticated { get; }

    Guid RequireUserId();
}
