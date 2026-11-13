using ChoyERP.Domain.Common;

namespace ChoyERP.Domain.Entities.Users;

/// <summary>
/// Acción autorizada sobre un recurso del ERP (por ejemplo <c>users.create</c>).
/// </summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
