using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Inventory.Dtos;
using ChoyERP.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Inventory;

public sealed class InventoryItemService(IAppDbContext db, IAuditService auditService)
{
    public async Task<PagedResult<InventoryItemDto>> GetPagedAsync(
        InventoryItemQuery query,
        CancellationToken cancellationToken)
    {
        var items = db.InventoryItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            items = items.Where(i =>
                i.Code.ToLower().Contains(search)
                || i.Name.ToLower().Contains(search)
                || (i.Description != null && i.Description.ToLower().Contains(search))
                || (i.Brand != null && i.Brand.ToLower().Contains(search))
                || (i.Model != null && i.Model.ToLower().Contains(search))
                || (i.Location != null && i.Location.ToLower().Contains(search)));
        }

        if (query.Category is not null)
        {
            items = items.Where(i => i.Category == query.Category);
        }

        if (query.Unit is not null)
        {
            items = items.Where(i => i.Unit == query.Unit);
        }

        if (query.IsActive is not null)
        {
            items = items.Where(i => i.IsActive == query.IsActive);
        }

        if (query.BelowMinimum == true)
        {
            items = items.Where(i => i.QuantityOnHand < i.MinimumStock);
        }
        else if (query.BelowMinimum == false)
        {
            items = items.Where(i => i.QuantityOnHand >= i.MinimumStock);
        }

        items = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? items.OrderByDescending(i => i.Code)
                : items.OrderBy(i => i.Code),
            "category" => query.SortDesc
                ? items.OrderByDescending(i => i.Category).ThenBy(i => i.Name)
                : items.OrderBy(i => i.Category).ThenBy(i => i.Name),
            "quantityonhand" => query.SortDesc
                ? items.OrderByDescending(i => i.QuantityOnHand)
                : items.OrderBy(i => i.QuantityOnHand),
            "minimumstock" => query.SortDesc
                ? items.OrderByDescending(i => i.MinimumStock)
                : items.OrderBy(i => i.MinimumStock),
            "createdat" => query.SortDesc
                ? items.OrderByDescending(i => i.CreatedAt)
                : items.OrderBy(i => i.CreatedAt),
            _ => query.SortDesc
                ? items.OrderByDescending(i => i.Name)
                : items.OrderBy(i => i.Name),
        };

        var page = await items.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<InventoryItemDto>(
            page.Items.Select(i => i.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<InventoryItemDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await GetItemOrThrowAsync(id, cancellationToken);
        return item.ToDto();
    }

    public async Task<InventoryItemDto> CreateAsync(
        CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(request.Code)
            ? await db.InventoryItems
                .Select(i => i.Code)
                .NextSequentialCodeAsync("ART-", cancellationToken)
            : request.Code.Trim().ToUpperInvariant();

        if (await db.InventoryItems.AnyAsync(i => i.Code == code, cancellationToken))
        {
            throw new ConflictException("Ya existe un artículo con ese código.");
        }

        var item = new InventoryItem
        {
            Code = code,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category,
            Unit = request.Unit,
            Brand = request.Brand?.Trim(),
            Model = request.Model?.Trim(),
            Location = request.Location?.Trim(),
            QuantityOnHand = 0,
            MinimumStock = request.MinimumStock,
            AverageUnitCost = request.UnitCost,
            IsActive = true,
            Notes = request.Notes?.Trim(),
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "inventory.items.created",
            nameof(InventoryItem),
            item.Id.ToString(),
            details: $"Artículo: {item.Code} - {item.Name}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(item.Id, cancellationToken);
    }

    public async Task<InventoryItemDto> UpdateAsync(
        Guid id,
        UpdateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await GetItemOrThrowAsync(id, cancellationToken);

        if (request.Unit != item.Unit
            && await db.InventoryMovements.AnyAsync(m => m.ItemId == id, cancellationToken))
        {
            throw new ConflictException(
                "No se puede cambiar la unidad de medida de un artículo con movimientos registrados. "
                + "Cree un artículo nuevo para la nueva unidad.");
        }

        item.Name = request.Name.Trim();
        item.Description = request.Description?.Trim();
        item.Category = request.Category;
        item.Unit = request.Unit;
        item.Brand = request.Brand?.Trim();
        item.Model = request.Model?.Trim();
        item.Location = request.Location?.Trim();
        item.MinimumStock = request.MinimumStock;
        item.Notes = request.Notes?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "inventory.items.updated",
            nameof(InventoryItem),
            item.Id.ToString(),
            details: $"Artículo: {item.Code} - {item.Name}. Mínimo: {item.MinimumStock} {item.Unit}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(item.Id, cancellationToken);
    }

    public async Task<InventoryItemDto> SetStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var item = await GetItemOrThrowAsync(id, cancellationToken);

        if (item.IsActive == isActive)
        {
            return item.ToDto();
        }

        if (!isActive && item.QuantityOnHand > 0)
        {
            throw new ConflictException(
                "No se puede desactivar un artículo con existencia. Registre una salida o un ajuste a cero primero.");
        }

        item.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            isActive ? "inventory.items.activated" : "inventory.items.deactivated",
            nameof(InventoryItem),
            item.Id.ToString(),
            details: $"Artículo: {item.Code} - {item.Name}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(item.Id, cancellationToken);
    }

    private async Task<InventoryItem> GetItemOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await db.InventoryItems.SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
        ?? throw new NotFoundException("Artículo de almacén", id);
}
