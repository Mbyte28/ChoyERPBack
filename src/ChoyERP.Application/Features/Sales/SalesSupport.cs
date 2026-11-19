using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Features.Sales.Dtos;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Sales;

internal static class SalesSupport
{
    public static DateOnly Today(IDateTimeProvider clock) =>
        DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

    public static decimal RoundMoney(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static async Task<Client> GetActiveClientAsync(
        IAppDbContext db,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var client = await db.Clients
            .SingleOrDefaultAsync(c => c.Id == clientId, cancellationToken)
            ?? throw new NotFoundException("Cliente", clientId);

        if (!client.IsActive)
        {
            throw new ConflictException("El cliente está inactivo y no puede registrar ventas.");
        }

        return client;
    }

    public static async Task EnsureProjectAcceptsSalesAsync(
        IAppDbContext db,
        Guid? projectId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        if (projectId is null)
        {
            return;
        }

        var project = await db.Projects
            .SingleOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new NotFoundException("Proyecto", projectId.Value);

        if (project.ClientId != clientId)
        {
            throw new ConflictException("El proyecto seleccionado no pertenece al cliente de la venta.");
        }

        if (project.Status == ProjectStatus.Cancelled)
        {
            throw new ConflictException("El proyecto está cancelado y no admite ventas.");
        }
    }

    public static async Task EnsureAddressBelongsToClientAsync(
        IAppDbContext db,
        Guid clientId,
        Guid? clientAddressId,
        CancellationToken cancellationToken)
    {
        if (clientAddressId is null)
        {
            return;
        }

        var belongs = await db.ClientAddresses
            .AnyAsync(a => a.Id == clientAddressId && a.ClientId == clientId, cancellationToken);

        if (!belongs)
        {
            throw new NotFoundException(
                "La dirección indicada no existe o no pertenece al cliente de la instalación.");
        }
    }

    public static async Task EnsureActiveWorkerAsync(
        IAppDbContext db,
        Guid? workerId,
        CancellationToken cancellationToken)
    {
        if (workerId is null)
        {
            return;
        }

        var worker = await db.Workers
            .SingleOrDefaultAsync(w => w.Id == workerId, cancellationToken)
            ?? throw new NotFoundException("Trabajador", workerId.Value);

        if (!worker.IsActive)
        {
            throw new ConflictException("El trabajador responsable está inactivo.");
        }
    }

    public static void EnsureNoDuplicateItems(IReadOnlyList<SaleItemRequest> items)
    {
        var duplicates = items
            .GroupBy(item => item.InventoryItemId)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicates.Length > 0)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Items"] =
                [
                    "Cada artículo debe aparecer una sola vez. Agrupe las cantidades en una única línea.",
                ],
            });
        }
    }

    public static void ValidateDates(
        DateOnly saleDate,
        DateOnly? expectedDeliveryDate,
        DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();

        if (saleDate > today)
        {
            errors["SaleDate"] = ["La fecha de venta no puede ser posterior a hoy."];
        }

        if (expectedDeliveryDate is not null && expectedDeliveryDate < saleDate)
        {
            errors["ExpectedDeliveryDate"] =
                ["La fecha de entrega prevista no puede ser anterior a la fecha de venta."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(errors);
        }
    }

    public static bool IsFinal(SaleStatus status) =>
        status is SaleStatus.Completed or SaleStatus.Cancelled;

    public static bool IsFinal(InstallationStatus status) =>
        status is InstallationStatus.Completed or InstallationStatus.Cancelled;
}
