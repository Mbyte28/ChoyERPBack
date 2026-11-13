using ChoyERP.Domain.Common;
using ChoyERP.Domain.Entities.Workers;

namespace ChoyERP.Domain.Entities.Users;

/// <summary>
/// Cuenta de acceso al ERP. Puede estar vinculada a un trabajador de CHOY.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Guid? WorkerId { get; set; }

    public Worker? Worker { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
