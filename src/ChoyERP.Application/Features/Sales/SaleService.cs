using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Sales.Dtos;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Inventory;
using ChoyERP.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Sales;

public sealed class SaleService(
    IAppDbContext db,
    IAuditService auditService,
    IDateTimeProvider clock)
{
    public async Task<PagedResult<SaleDto>> GetPagedAsync(
        SaleQuery query,
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

        var sales = SaleQueryWithDetails().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            sales = sales.Where(s =>
                s.Code.ToLower().Contains(search)
                || s.Client.Name.ToLower().Contains(search)
                || (s.Project != null && s.Project.Code.ToLower().Contains(search))
                || (s.Project != null && s.Project.Name.ToLower().Contains(search))
                || (s.Notes != null && s.Notes.ToLower().Contains(search)));
        }

        if (query.Status is not null)
        {
            sales = sales.Where(s => s.Status == query.Status);
        }

        if (query.ClientId is not null)
        {
            sales = sales.Where(s => s.ClientId == query.ClientId);
        }

        if (query.ProjectId is not null)
        {
            sales = sales.Where(s => s.ProjectId == query.ProjectId);
        }

        if (query.FromDate is not null)
        {
            sales = sales.Where(s => s.SaleDate >= query.FromDate);
        }

        if (query.ToDate is not null)
        {
            sales = sales.Where(s => s.SaleDate <= query.ToDate);
        }

        sales = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? sales.OrderByDescending(s => s.Code)
                : sales.OrderBy(s => s.Code),
            "saledate" => query.SortDesc
                ? sales.OrderByDescending(s => s.SaleDate).ThenByDescending(s => s.Code)
                : sales.OrderBy(s => s.SaleDate).ThenBy(s => s.Code),
            "status" => query.SortDesc
                ? sales.OrderByDescending(s => s.Status)
                : sales.OrderBy(s => s.Status),
            "total" => query.SortDesc
                ? sales.OrderByDescending(s => s.Total)
                : sales.OrderBy(s => s.Total),
            "clientname" => query.SortDesc
                ? sales.OrderByDescending(s => s.Client.Name)
                : sales.OrderBy(s => s.Client.Name),
            _ => query.SortDesc
                ? sales.OrderByDescending(s => s.CreatedAt).ThenByDescending(s => s.Code)
                : sales.OrderBy(s => s.CreatedAt).ThenBy(s => s.Code),
        };

        var page = await sales.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<SaleDto>(
            page.Items.Select(s => s.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<SaleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var sale = await GetSaleOrThrowAsync(id, cancellationToken);
        return sale.ToDto();
    }

    public async Task<SaleDto> CreateAsync(
        CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var items = ValidateLines(request.Items);
        var today = SalesSupport.Today(clock);
        var saleDate = request.SaleDate ?? today;

        SalesSupport.ValidateDates(saleDate, request.ExpectedDeliveryDate, today);

        var client = await SalesSupport.GetActiveClientAsync(db, request.ClientId, cancellationToken);

        await SalesSupport.EnsureProjectAcceptsSalesAsync(
            db, request.ProjectId, client.Id, cancellationToken);

        var inventoryItems = await LoadInventoryItemsAsync(items, cancellationToken);

        var sale = new Sale
        {
            Code = await db.Sales
                .Select(s => s.Code)
                .NextSequentialCodeAsync("VEN-", cancellationToken),
            ClientId = client.Id,
            ProjectId = request.ProjectId,
            Status = SaleStatus.Draft,
            Currency = request.Currency.Trim().ToUpperInvariant(),
            TaxRate = request.TaxRate,
            SaleDate = saleDate,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            Notes = request.Notes?.Trim(),
        };

        ApplyItems(sale, items, inventoryItems);
        sale.RecalculateTotals();

        db.Sales.Add(sale);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "sales.created",
            nameof(Sale),
            sale.Id.ToString(),
            details: $"Venta: {sale.Code} - {client.Name}. Total: {sale.Total} {sale.Currency}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(sale.Id, cancellationToken);
    }

    public async Task<SaleDto> UpdateAsync(
        Guid id,
        UpdateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var sale = await GetSaleOrThrowAsync(id, cancellationToken);

        if (!sale.IsEditable)
        {
            throw new ConflictException(
                "Solo se puede editar una venta en borrador. Las ventas confirmadas conservan "
                + "sus líneas y su stock para preservar el historial.");
        }

        var items = ValidateLines(request.Items);
        var today = SalesSupport.Today(clock);
        var saleDate = request.SaleDate ?? sale.SaleDate;

        SalesSupport.ValidateDates(saleDate, request.ExpectedDeliveryDate, today);

        var client = await SalesSupport.GetActiveClientAsync(db, request.ClientId, cancellationToken);

        await SalesSupport.EnsureProjectAcceptsSalesAsync(
            db, request.ProjectId, client.Id, cancellationToken);

        var inventoryItems = await LoadInventoryItemsAsync(items, cancellationToken);

        sale.ClientId = client.Id;
        sale.ProjectId = request.ProjectId;
        sale.Currency = request.Currency.Trim().ToUpperInvariant();
        sale.TaxRate = request.TaxRate;
        sale.SaleDate = saleDate;
        sale.ExpectedDeliveryDate = request.ExpectedDeliveryDate;
        sale.Notes = request.Notes?.Trim();

        // Las líneas existentes se eliminan explícitamente del contexto y las nuevas
        // se marcan como Added; reciclar instancias sobre la colección rastreada
        // haría que EF intentara actualizar filas que todavía no existen.
        db.SaleItems.RemoveRange(sale.Items.ToArray());
        sale.Items.Clear();

        var newItems = BuildItems(sale, items, inventoryItems);

        foreach (var item in newItems)
        {
            sale.Items.Add(item);
        }

        db.SaleItems.AddRange(newItems);
        sale.RecalculateTotals();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "sales.updated",
            nameof(Sale),
            sale.Id.ToString(),
            details: $"Venta: {sale.Code} - {client.Name}. Total: {sale.Total} {sale.Currency}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(sale.Id, cancellationToken);
    }

    public async Task<SaleDto> ConfirmAsync(Guid id, CancellationToken cancellationToken)
    {
        var sale = await GetSaleOrThrowAsync(id, cancellationToken);

        if (sale.Status != SaleStatus.Draft)
        {
            throw new ConflictException(
                $"La venta está en estado '{sale.Status}' y no se puede confirmar.");
        }

        if (sale.Items.Count == 0)
        {
            throw new ConflictException("La venta debe tener al menos una línea para confirmarse.");
        }

        if (!sale.Client.IsActive)
        {
            throw new ConflictException("El cliente está inactivo y no puede confirmar la venta.");
        }

        if (sale.Project is { Status: ProjectStatus.Cancelled })
        {
            throw new ConflictException("El proyecto está cancelado y no admite ventas.");
        }

        var groups = sale.Items
            .GroupBy(item => item.InventoryItem)
            .Select(group => new
            {
                Item = group.Key,
                Quantity = group.Sum(item => item.Quantity),
            })
            .ToArray();

        foreach (var group in groups)
        {
            if (!group.Item.IsActive)
            {
                throw new ConflictException(
                    $"El artículo '{group.Item.Code} - {group.Item.Name}' está inactivo "
                    + "y no puede venderse.");
            }

            if (group.Quantity > group.Item.QuantityOnHand)
            {
                throw new ConflictException(
                    $"Stock insuficiente del artículo '{group.Item.Code} - {group.Item.Name}'. "
                    + $"Disponible: {group.Item.QuantityOnHand} {group.Item.Unit}; "
                    + $"solicitado: {group.Quantity} {group.Item.Unit}.");
            }
        }

        var nextMovementCode = await db.InventoryMovements
            .Select(m => m.Code)
            .NextSequentialCodeAsync("MOV-", cancellationToken);
        var nextMovementNumber = int.Parse(nextMovementCode["MOV-".Length..]);

        var today = SalesSupport.Today(clock);
        var movements = new List<InventoryMovement>();

        foreach (var group in groups)
        {
            var stockBefore = group.Item.QuantityOnHand;
            group.Item.QuantityOnHand = stockBefore - group.Quantity;

            var movement = new InventoryMovement
            {
                Code = $"MOV-{nextMovementNumber++:D5}",
                ItemId = group.Item.Id,
                Type = InventoryMovementType.Exit,
                MovementDate = today,
                Quantity = group.Quantity,
                QuantityDelta = -group.Quantity,
                StockBefore = stockBefore,
                StockAfter = group.Item.QuantityOnHand,
                UnitCost = group.Item.AverageUnitCost,
                TotalCost = SalesSupport.RoundMoney(-group.Quantity * group.Item.AverageUnitCost),
                Reason = "Venta de equipos",
                Reference = sale.Code,
                SaleId = sale.Id,
                Notes = $"Salida automática al confirmar la venta {sale.Code}.",
            };

            db.InventoryMovements.Add(movement);
            movements.Add(movement);
        }

        sale.Status = SaleStatus.Confirmed;
        sale.ConfirmedAt = clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "sales.confirmed",
            nameof(Sale),
            sale.Id.ToString(),
            details:
                $"Venta confirmada: {sale.Code}. Total: {sale.Total} {sale.Currency}. "
                + $"Salidas de stock: {movements.Count}.",
            cancellationToken: cancellationToken);

        foreach (var movement in movements)
        {
            await auditService.LogAsync(
                "inventory.movements.created",
                nameof(InventoryMovement),
                movement.Id.ToString(),
                details:
                    $"Movimiento {movement.Code} (salida por venta) del artículo "
                    + $"{movement.ItemId}: {movement.StockBefore} -> {movement.StockAfter}.",
                cancellationToken: cancellationToken);
        }

        return await GetByIdAsync(sale.Id, cancellationToken);
    }

    public async Task<SaleDto> DeliverAsync(
        Guid id,
        DeliverSaleRequest request,
        CancellationToken cancellationToken)
    {
        var sale = await GetSaleOrThrowAsync(id, cancellationToken);

        if (sale.Status != SaleStatus.Confirmed)
        {
            throw new ConflictException(
                "Solo se puede entregar una venta confirmada y pendiente de entrega.");
        }

        var today = SalesSupport.Today(clock);
        var deliveredDate = request.DeliveredDate ?? today;

        if (deliveredDate > today)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["DeliveredDate"] = ["La fecha de entrega no puede ser posterior a hoy."],
            });
        }

        if (deliveredDate < sale.SaleDate)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["DeliveredDate"] = ["La fecha de entrega no puede ser anterior a la fecha de venta."],
            });
        }

        sale.DeliveredDate = deliveredDate;
        sale.Status = SaleStatus.Delivered;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            sale.Notes = request.Notes.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "sales.delivered",
            nameof(Sale),
            sale.Id.ToString(),
            details: $"Venta entregada: {sale.Code}. Fecha: {deliveredDate}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(sale.Id, cancellationToken);
    }

    public async Task<SaleDto> CompleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var sale = await GetSaleOrThrowAsync(id, cancellationToken);

        if (sale.Status != SaleStatus.Delivered)
        {
            throw new ConflictException(
                "Solo se puede cerrar una venta entregada.");
        }

        if (sale.Installation is
            { Status: InstallationStatus.Scheduled }
            or { Status: InstallationStatus.InProgress })
        {
            throw new ConflictException(
                "La instalación asociada sigue pendiente. Complétela antes de cerrar la venta.");
        }

        sale.Status = SaleStatus.Completed;
        sale.CompletedAt = clock.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "sales.completed",
            nameof(Sale),
            sale.Id.ToString(),
            details: $"Venta cerrada: {sale.Code}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(sale.Id, cancellationToken);
    }

    public async Task<SaleDto> CancelAsync(
        Guid id,
        string reason,
        CancellationToken cancellationToken)
    {
        var sale = await GetSaleOrThrowAsync(id, cancellationToken);

        if (sale.Status != SaleStatus.Draft)
        {
            throw new ConflictException(
                "Solo se puede anular una venta en borrador. Si el stock ya salió, "
                + "registre el ingreso de los equipos como una entrada de almacén.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Reason"] = ["El motivo de anulación es obligatorio."],
            });
        }

        sale.Status = SaleStatus.Cancelled;
        sale.CancelledAt = clock.UtcNow;
        sale.CancellationReason = reason.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "sales.cancelled",
            nameof(Sale),
            sale.Id.ToString(),
            details: $"Venta anulada: {sale.Code}. Motivo: {sale.CancellationReason}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(sale.Id, cancellationToken);
    }

    private static IReadOnlyList<SaleItemRequest> ValidateLines(
        IReadOnlyList<SaleItemRequest>? items)
    {
        var errors = new Dictionary<string, string[]>();

        if (items is null || items.Count == 0)
        {
            errors["Items"] = ["La venta debe tener al menos una línea."];
        }
        else if (items.Count > 200)
        {
            errors["Items"] = ["La venta no puede tener más de 200 líneas."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(errors);
        }

        SalesSupport.EnsureNoDuplicateItems(items!);

        return items!;
    }

    private async Task<Dictionary<Guid, InventoryItem>> LoadInventoryItemsAsync(
        IReadOnlyList<SaleItemRequest> items,
        CancellationToken cancellationToken)
    {
        var ids = items.Select(item => item.InventoryItemId).Distinct().ToArray();

        var inventoryItems = await db.InventoryItems
            .Where(item => ids.Contains(item.Id))
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            var item = inventoryItems.SingleOrDefault(i => i.Id == id)
                ?? throw new NotFoundException("Artículo de almacén", id);

            if (!item.IsActive)
            {
                throw new ConflictException(
                    $"El artículo '{item.Code} - {item.Name}' está inactivo y no puede venderse.");
            }
        }

        return inventoryItems.ToDictionary(item => item.Id);
    }

    private static void ApplyItems(
        Sale sale,
        IReadOnlyList<SaleItemRequest> items,
        Dictionary<Guid, InventoryItem> inventoryItems)
    {
        foreach (var item in BuildItems(sale, items, inventoryItems))
        {
            sale.Items.Add(item);
        }
    }

    private static List<SaleItem> BuildItems(
        Sale sale,
        IReadOnlyList<SaleItemRequest> items,
        Dictionary<Guid, InventoryItem> inventoryItems)
    {
        var result = new List<SaleItem>(items.Count);

        for (var index = 0; index < items.Count; index++)
        {
            var line = items[index];
            var item = inventoryItems[line.InventoryItemId];

            result.Add(new SaleItem
            {
                SaleId = sale.Id,
                InventoryItemId = item.Id,
                InventoryItem = item,
                Description = string.IsNullOrWhiteSpace(item.Model)
                    ? item.Name
                    : $"{item.Name} ({item.Model})",
                Unit = item.Unit.ToString(),
                Quantity = line.Quantity,
                UnitPrice = SalesSupport.RoundMoney(line.UnitPrice),
                SortOrder = index,
            });
        }

        return result;
    }

    private IQueryable<Sale> SaleQueryWithDetails() =>
        db.Sales
            .Include(s => s.Client)
            .Include(s => s.Project)
            .Include(s => s.Installation)
            .Include(s => s.Items)
            .ThenInclude(item => item.InventoryItem);

    private async Task<Sale> GetSaleOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await SaleQueryWithDetails()
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
        ?? throw new NotFoundException("Venta", id);
}
