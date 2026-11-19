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
                Permissions.Inventory.Read,
                Permissions.Sales.Read,
                Permissions.Sales.Create,
                Permissions.Sales.Update,
                Permissions.Sales.Confirm,
                Permissions.Sales.Complete,
                Permissions.Sales.Cancel,
                Permissions.Installations.Read,
                Permissions.Installations.Create,
                Permissions.Installations.Update,
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
                Permissions.Inventory.Read,
                Permissions.Sales.Read,
                Permissions.Installations.Read,
            ]),
        new(
            RoleNames.Warehouse,
            "Almacén y movimientos de inventario.",
            [
                Permissions.Clients.Read,
                Permissions.Projects.Read,
                Permissions.Inventory.Read,
                Permissions.Inventory.Create,
                Permissions.Inventory.Update,
                Permissions.Inventory.Deactivate,
                Permissions.Inventory.Move,
                Permissions.Sales.Read,
                Permissions.Sales.Deliver,
                Permissions.Installations.Read,
            ]),
        new(
            RoleNames.Technician,
            "Seguimiento de equipos, inspecciones y mantenimiento.",
            [
                Permissions.Clients.Read,
                Permissions.Workers.Read,
                Permissions.Projects.Read,
                Permissions.Inventory.Read,
                Permissions.Sales.Read,
                Permissions.Installations.Read,
                Permissions.Installations.Update,
                Permissions.Installations.Complete,
            ]),
    ];
}

public sealed record RoleDefinition(
    string Name,
    string Description,
    IReadOnlyList<string> PermissionCodes,
    bool IsSystemRole = true);
