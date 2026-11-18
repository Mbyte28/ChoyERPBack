using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Workers;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Contracting;

internal static class ContractingSupport
{
    public static DateOnly Today(IDateTimeProvider clock) =>
        DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

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
            throw new ConflictException("El cliente está inactivo y no puede usarse en contrataciones.");
        }

        return client;
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
                "La dirección indicada no existe o no pertenece al cliente seleccionado.");
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

    public static bool IsFinal(ProjectRequestStatus status) =>
        status is ProjectRequestStatus.Accepted
            or ProjectRequestStatus.Rejected
            or ProjectRequestStatus.Cancelled;

    public static bool IsFinal(QuoteStatus status) =>
        status is QuoteStatus.Accepted
            or QuoteStatus.Rejected
            or QuoteStatus.Expired
            or QuoteStatus.Cancelled;

    public static bool IsFinal(ContractStatus status) =>
        status is ContractStatus.Completed or ContractStatus.Cancelled;

    public static bool IsFinal(ProjectStatus status) =>
        status is ProjectStatus.Completed or ProjectStatus.Cancelled;
}
