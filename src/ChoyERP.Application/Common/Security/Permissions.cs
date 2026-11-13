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
    ];
}

public sealed record PermissionDefinition(string Code, string Name, string Module);
