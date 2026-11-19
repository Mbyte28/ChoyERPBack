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

    public static class Inventory
    {
        public const string Read = "inventory.read";
        public const string Create = "inventory.create";
        public const string Update = "inventory.update";
        public const string Deactivate = "inventory.deactivate";
        public const string Move = "inventory.move";
    }

    public static class Sales
    {
        public const string Read = "sales.read";
        public const string Create = "sales.create";
        public const string Update = "sales.update";
        public const string Confirm = "sales.confirm";
        public const string Deliver = "sales.deliver";
        public const string Complete = "sales.complete";
        public const string Cancel = "sales.cancel";
    }

    public static class Installations
    {
        public const string Read = "installations.read";
        public const string Create = "installations.create";
        public const string Update = "installations.update";
        public const string Complete = "installations.complete";
        public const string Cancel = "installations.cancel";
    }

    public static class Equipment
    {
        public const string Read = "equipment.read";
        public const string Create = "equipment.create";
        public const string Update = "equipment.update";
        public const string ChangeStatus = "equipment.change_status";
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
        new(Inventory.Read, "Consultar artículos, existencias y movimientos", "Almacén"),
        new(Inventory.Create, "Registrar artículos de almacén", "Almacén"),
        new(Inventory.Update, "Editar artículos y niveles mínimos", "Almacén"),
        new(Inventory.Deactivate, "Activar o desactivar artículos sin existencia", "Almacén"),
        new(Inventory.Move, "Registrar entradas, salidas y ajustes de inventario", "Almacén"),
        new(Sales.Read, "Consultar ventas de equipos", "Ventas"),
        new(Sales.Create, "Registrar ventas de equipos", "Ventas"),
        new(Sales.Update, "Editar ventas en borrador", "Ventas"),
        new(Sales.Confirm, "Confirmar ventas y descontar stock", "Ventas"),
        new(Sales.Deliver, "Registrar la entrega de equipos vendidos", "Ventas"),
        new(Sales.Complete, "Cerrar ventas entregadas", "Ventas"),
        new(Sales.Cancel, "Anular ventas en borrador", "Ventas"),
        new(Installations.Read, "Consultar instalaciones", "Instalaciones"),
        new(Installations.Create, "Programar instalaciones de ventas o proyectos", "Instalaciones"),
        new(Installations.Update, "Editar instalaciones y asignar técnicos", "Instalaciones"),
        new(Installations.Complete, "Iniciar y completar instalaciones", "Instalaciones"),
        new(Installations.Cancel, "Anular instalaciones pendientes", "Instalaciones"),
        new(Equipment.Read, "Consultar equipos instalados y su historial", "Equipos"),
        new(Equipment.Create, "Registrar equipos instalados", "Equipos"),
        new(Equipment.Update, "Editar la ficha de un equipo instalado", "Equipos"),
        new(Equipment.ChangeStatus, "Cambiar el estado operativo de un equipo", "Equipos"),
    ];
}

public sealed record PermissionDefinition(string Code, string Name, string Module);
