using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Domain.Entities.Contracting;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Inventory;

internal static class InventorySupport
{
    public static DateOnly Today(IDateTimeProvider clock) =>
        DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

    /// <summary>
    /// Valida que el proyecto exista y admita movimientos de inventario.
    /// Un proyecto cancelado ya no consume materiales.
    /// </summary>
    public static async Task EnsureProjectAcceptsMovementsAsync(
        IAppDbContext db,
        Guid? projectId,
        CancellationToken cancellationToken)
    {
        if (projectId is null)
        {
            return;
        }

        var project = await db.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == projectId, cancellationToken)
            ?? throw new NotFoundException("Proyecto", projectId.Value);

        if (project.Status == ProjectStatus.Cancelled)
        {
            throw new ConflictException(
                "El proyecto está cancelado y no admite movimientos de inventario.");
        }
    }
}
