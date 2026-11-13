using ChoyERP.Domain.Common;

namespace ChoyERP.Domain.Entities.Users;

/// <summary>
/// Agrupación de permisos que se asigna a las cuentas de usuario.
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Los roles del sistema los administra el backend y no se pueden renombrar ni eliminar.
    /// </summary>
    public bool IsSystemRole { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
