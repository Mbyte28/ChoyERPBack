namespace ChoyERP.Application.Common.Security;

/// <summary>
/// Catálogo central de permisos. Es la fuente de verdad para la autorización del backend.
/// </summary>
public static class Permissions
{
    public static class Users
    {
        public const string Read = "users.read";
        public const string Create = "users.create";
        public const string Update = "users.update";
        public const string Deactivate = "users.deactivate";
        public const string ResetPassword = "users.reset_password";
    }

    public static class Roles
    {
        public const string Read = "roles.read";
        public const string Manage = "roles.manage";
    }

    public static class Workers
    {
        public const string Read = "workers.read";
        public const string Create = "workers.create";
        public const string Update = "workers.update";
        public const string Deactivate = "workers.deactivate";
    }

    public static class Clients
    {
        public const string Read = "clients.read";
        public const string Create = "clients.create";
        public const string Update = "clients.update";
        public const string Deactivate = "clients.deactivate";
    }

    public static class ProjectRequests
    {
        public const string Read = "requests.read";
        public const string Create = "requests.create";
        public const string Update = "requests.update";
        public const string Cancel = "requests.cancel";
    }

    public static class Quotes
    {
        public const string Read = "quotes.read";
        public const string Create = "quotes.create";
        public const string Update = "quotes.update";
        public const string Send = "quotes.send";
        public const string Decide = "quotes.decide";
    }

    public static class Contracts
    {
        public const string Read = "contracts.read";
        public const string Create = "contracts.create";
        public const string Update = "contracts.update";
    }

    public static class Projects
    {
        public const string Read = "projects.read";
        public const string Create = "projects.create";
        public const string Update = "projects.update";
    }

    public static IReadOnlyList<PermissionDefinition> All { get; } =
    [
        new(Users.Read, "Consultar usuarios", "Usuarios"),
        new(Users.Create, "Crear usuarios", "Usuarios"),
        new(Users.Update, "Editar usuarios y asignar roles", "Usuarios"),
        new(Users.Deactivate, "Activar o desactivar usuarios", "Usuarios"),
        new(Users.ResetPassword, "Restablecer contraseñas de usuarios", "Usuarios"),
        new(Roles.Read, "Consultar roles y permisos", "Roles"),
        new(Roles.Manage, "Crear, editar y eliminar roles", "Roles"),
        new(Workers.Read, "Consultar trabajadores", "Trabajadores"),
        new(Workers.Create, "Registrar trabajadores", "Trabajadores"),
        new(Workers.Update, "Editar trabajadores", "Trabajadores"),
        new(Workers.Deactivate, "Activar o desactivar trabajadores", "Trabajadores"),
        new(Clients.Read, "Consultar clientes", "Clientes"),
        new(Clients.Create, "Registrar clientes", "Clientes"),
        new(Clients.Update, "Editar clientes y sus direcciones", "Clientes"),
        new(Clients.Deactivate, "Activar o desactivar clientes", "Clientes"),
        new(ProjectRequests.Read, "Consultar solicitudes de proyecto", "Solicitudes"),
        new(ProjectRequests.Create, "Registrar solicitudes de proyecto", "Solicitudes"),
        new(ProjectRequests.Update, "Editar solicitudes de proyecto", "Solicitudes"),
        new(ProjectRequests.Cancel, "Rechazar o cancelar solicitudes", "Solicitudes"),
        new(Quotes.Read, "Consultar cotizaciones", "Cotizaciones"),
        new(Quotes.Create, "Crear cotizaciones", "Cotizaciones"),
        new(Quotes.Update, "Editar y anular cotizaciones en borrador", "Cotizaciones"),
        new(Quotes.Send, "Enviar cotizaciones al cliente", "Cotizaciones"),
        new(Quotes.Decide, "Aceptar o rechazar cotizaciones", "Cotizaciones"),
        new(Contracts.Read, "Consultar contratos", "Contratos"),
        new(Contracts.Create, "Generar contratos desde cotizaciones aceptadas", "Contratos"),
        new(Contracts.Update, "Editar y cambiar el estado de contratos", "Contratos"),
        new(Projects.Read, "Consultar proyectos", "Proyectos"),
        new(Projects.Create, "Crear proyectos desde contratos vigentes", "Proyectos"),
        new(Projects.Update, "Editar proyectos, avance y estado", "Proyectos"),
    ];
}

public sealed record PermissionDefinition(string Code, string Name, string Module);
