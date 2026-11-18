namespace ChoyERP.Application.Common.Security;

public static class RoleNames
{
    public const string Administrator = "Administrador";
    public const string Commercial = "Comercial";
    public const string Engineering = "Ingeniería";
    public const string Warehouse = "Almacenero";
    public const string Technician = "Técnico";
}

/// <summary>
/// Roles iniciales propuestos en la especificación. Se siembran al crear la base de datos.
/// </summary>
public static class RoleCatalog
{
    public static IReadOnlyList<RoleDefinition> All { get; } =
    [
        new(
            RoleNames.Administrator,
            "Acceso total a todos los módulos y a la gestión de usuarios.",
            Permissions.All.Select(p => p.Code).ToArray()),
        new(
            RoleNames.Commercial,
            "Contrataciones y venta de equipos.",
            [
                Permissions.Clients.Read,
                Permissions.Clients.Create,
                Permissions.Clients.Update,
                Permissions.ProjectRequests.Read,
                Permissions.ProjectRequests.Create,
                Permissions.ProjectRequests.Update,
                Permissions.ProjectRequests.Cancel,
                Permissions.Quotes.Read,
                Permissions.Quotes.Create,
                Permissions.Quotes.Update,
                Permissions.Quotes.Send,
                Permissions.Quotes.Decide,
                Permissions.Contracts.Read,
                Permissions.Contracts.Create,
                Permissions.Contracts.Update,
                Permissions.Projects.Read,
                Permissions.Projects.Create,
            ]),
        new(
            RoleNames.Engineering,
            "Sistemas de refrigeración y documentación técnica.",
            [
                Permissions.Clients.Read,
                Permissions.Workers.Read,
                Permissions.ProjectRequests.Read,
                Permissions.Quotes.Read,
                Permissions.Contracts.Read,
                Permissions.Projects.Read,
                Permissions.Projects.Update,
            ]),
        new(
            RoleNames.Warehouse,
            "Almacén y movimientos de inventario.",
            [Permissions.Clients.Read]),
        new(
            RoleNames.Technician,
            "Seguimiento de equipos, inspecciones y mantenimiento.",
            [Permissions.Clients.Read, Permissions.Workers.Read, Permissions.Projects.Read]),
    ];
}

public sealed record RoleDefinition(
    string Name,
    string Description,
    IReadOnlyList<string> PermissionCodes,
    bool IsSystemRole = true);
