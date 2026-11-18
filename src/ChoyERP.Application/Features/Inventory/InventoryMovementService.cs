using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Inventory.Dtos;
using ChoyERP.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Inventory;

public sealed class InventoryMovementService(
    IAppDbContext db,
    IAuditService auditService,
    IDateTimeProvider clock)
{
    public async Task<PagedResult<InventoryMovementDto>> GetPagedAsync(
        InventoryMovementQuery query,
        CancellationToken cancellationToken)
    {
        if (query.FromDate is not null
            && query.ToDate is not null
            && query.ToDate < query.FromDate)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["FromDate"] = ["La fecha inicial no puede ser posterior a la fecha final."],
            });
        }

        var movements = MovementQueryWithDetails().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            movements = movements.Where(m =>
                m.Code.ToLower().Contains(search)
                || m.Item.Code.ToLower().Contains(search)
                || m.Item.Name.ToLower().Contains(search)
                || (m.Reference != null && m.Reference.ToLower().Contains(search))
                || (m.Reason != null && m.Reason.ToLower().Contains(search)));
        }

        if (query.ItemId is not null)
        {
            movements = movements.Where(m => m.ItemId == query.ItemId);
        }

        if (query.Type is not null)
        {
            movements = movements.Where(m => m.Type == query.Type);
        }

        if (query.ProjectId is not null)
        {
            movements = movements.Where(m => m.ProjectId == query.ProjectId);
        }

        if (query.FromDate is not null)
        {
            movements = movements.Where(m => m.MovementDate >= query.FromDate);
        }

        if (query.ToDate is not null)
        {
            movements = movements.Where(m => m.MovementDate <= query.ToDate);
        }

        movements = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? movements.OrderByDescending(m => m.Code)
                : movements.OrderBy(m => m.Code),
            "type" => query.SortDesc
                ? movements.OrderByDescending(m => m.Type).ThenByDescending(m => m.MovementDate)
                : movements.OrderBy(m => m.Type).ThenBy(m => m.MovementDate),
            "quantity" => query.SortDesc
                ? movements.OrderByDescending(m => m.Quantity)
                : movements.OrderBy(m => m.Quantity),
            "createdat" => query.SortDesc
                ? movements.OrderByDescending(m => m.CreatedAt)
                : movements.OrderBy(m => m.CreatedAt),
            "movementdate" => query.SortDesc
                ? movements.OrderByDescending(m => m.MovementDate).ThenByDescending(m => m.Code)
                : movements.OrderBy(m => m.MovementDate).ThenBy(m => m.Code),
            _ => movements.OrderByDescending(m => m.MovementDate).ThenByDescending(m => m.Code),
        };

        var page = await movements.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<InventoryMovementDto>(
            page.Items.Select(m => m.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<InventoryMovementDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var movement = await MovementQueryWithDetails()
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.Id == id, cancellationToken)
            ?? throw new NotFoundException("Movimiento de inventario", id);

        return movement.ToDto();
    }

    public async Task<InventoryMovementDto> CreateAsync(
        CreateInventoryMovementRequest request,
        CancellationToken cancellationToken)
    {
        ValidateQuantities(request);

        var today = InventorySupport.Today(clock);
        var movementDate = request.MovementDate ?? today;

        if (movementDate > today)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["MovementDate"] = ["La fecha del movimiento no puede ser posterior a hoy."],
            });
        }

        await InventorySupport.EnsureProjectAcceptsMovementsAsync(
            db, request.ProjectId, cancellationToken);

        var item = await db.InventoryItems
            .SingleOrDefaultAsync(i => i.Id == request.ItemId, cancellationToken)
            ?? throw new NotFoundException("Artículo de almacén", request.ItemId);

        if (!item.IsActive)
        {
            throw new ConflictException(
                "El artículo está inactivo y no admite movimientos de inventario.");
        }

        var stockBefore = item.QuantityOnHand;
        var (stockAfter, unitCost) = ApplyMovement(item, request);

        var movement = new InventoryMovement
        {
            Code = await db.InventoryMovements
                .Select(m => m.Code)
                .NextSequentialCodeAsync("MOV-", cancellationToken),
            ItemId = item.Id,
            Type = request.Type,
            MovementDate = movementDate,
            Quantity = request.Quantity,
            QuantityDelta = stockAfter - stockBefore,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            UnitCost = unitCost,
            TotalCost = RoundMoney((stockAfter - stockBefore) * unitCost),
            Reason = request.Reason?.Trim(),
            Reference = request.Reference?.Trim(),
            ProjectId = request.ProjectId,
            Notes = request.Notes?.Trim(),
        };

        db.InventoryMovements.Add(movement);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "inventory.movements.created",
            nameof(InventoryMovement),
            movement.Id.ToString(),
            details:
                $"Movimiento {movement.Code} ({TypeLabel(movement.Type)}) del artículo {item.Code}: "
                + $"{movement.StockBefore} -> {movement.StockAfter} {item.Unit}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(movement.Id, cancellationToken);
    }

    /// <summary>
    /// Aplica el movimiento al artículo y devuelve la existencia resultante junto
    /// con el costo unitario utilizado.
    /// </summary>
    private static (decimal StockAfter, decimal UnitCost) ApplyMovement(
        InventoryItem item,
        CreateInventoryMovementRequest request)
    {
        var stockBefore = item.QuantityOnHand;

        switch (request.Type)
        {
            case InventoryMovementType.Entry:
            {
                var unitCost = RoundCost(request.UnitCost ?? item.AverageUnitCost);
                var stockAfter = stockBefore + request.Quantity;

                item.AverageUnitCost = RoundCost(
                    ((stockBefore * item.AverageUnitCost) + (request.Quantity * unitCost)) / stockAfter);
                item.QuantityOnHand = stockAfter;

                return (stockAfter, unitCost);
            }

            case InventoryMovementType.Exit:
            {
                if (request.Quantity > stockBefore)
                {
                    throw new ConflictException(
                        $"Stock insuficiente del artículo '{item.Code} - {item.Name}'. "
                        + $"Disponible: {stockBefore} {item.Unit}; solicitado: {request.Quantity} {item.Unit}.");
                }

                item.QuantityOnHand = stockBefore - request.Quantity;

                return (item.QuantityOnHand, item.AverageUnitCost);
            }

            case InventoryMovementType.Adjustment:
            {
                if (request.Quantity == stockBefore)
                {
                    throw new ConflictException(
                        "El ajuste no modifica la existencia del artículo; no se registra el movimiento.");
                }

                item.QuantityOnHand = request.Quantity;

                return (item.QuantityOnHand, item.AverageUnitCost);
            }

            default:
                throw new ValidationAppException(new Dictionary<string, string[]>
                {
                    ["Type"] = ["El tipo de movimiento no es válido."],
                });
        }
    }

    private static void ValidateQuantities(CreateInventoryMovementRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Quantity < 0)
        {
            errors["Quantity"] = ["La cantidad no puede ser negativa."];
        }
        else if (request.Type != InventoryMovementType.Adjustment && request.Quantity == 0)
        {
            errors["Quantity"] = ["La cantidad debe ser mayor que cero."];
        }

        if (request.UnitCost is < 0)
        {
            errors["UnitCost"] = ["El costo unitario no puede ser negativo."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(errors);
        }
    }

    private static decimal RoundCost(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string TypeLabel(InventoryMovementType type) => type switch
    {
        InventoryMovementType.Entry => "entrada",
        InventoryMovementType.Exit => "salida",
        _ => "ajuste",
    };

    private IQueryable<InventoryMovement> MovementQueryWithDetails() =>
        db.InventoryMovements
            .Include(m => m.Item)
            .Include(m => m.Project);
}
