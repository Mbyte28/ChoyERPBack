using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Inventory;
using ChoyERP.Application.Features.Inventory.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Catálogo de artículos del almacén y sus existencias.</summary>
[ApiController]
[Authorize]
[Route("api/inventory/items")]
[Produces("application/json")]
public sealed class InventoryItemsController(
    InventoryItemService itemService,
    InventoryMovementService movementService) : ControllerBase
{
    /// <summary>
    /// Lista artículos con paginación, búsqueda, filtros y ordenamiento.
    /// Use <c>belowMinimum=true</c> para ver los artículos bajo el stock mínimo.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Inventory.Read)]
    [ProducesResponseType<PagedResult<InventoryItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<InventoryItemDto>>> GetAll(
        [FromQuery] InventoryItemQuery query,
        CancellationToken cancellationToken) =>
        Ok(await itemService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene un artículo con su existencia, costo promedio y alerta de stock.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Inventory.Read)]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItemDto>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await itemService.GetByIdAsync(id, cancellationToken));

    /// <summary>Registra un artículo. El código se genera automáticamente si no se envía.</summary>
    [HttpPost]
    [HasPermission(Permissions.Inventory.Create)]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InventoryItemDto>> Create(
        [FromBody] CreateInventoryItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await itemService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    /// <summary>
    /// Edita un artículo y su stock mínimo. La existencia y el costo promedio
    /// solo cambian mediante movimientos de inventario.
    /// </summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Inventory.Update)]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InventoryItemDto>> Update(
        Guid id,
        [FromBody] UpdateInventoryItemRequest request,
        CancellationToken cancellationToken) =>
        Ok(await itemService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Activa o desactiva un artículo. No se permite desactivar con existencia.</summary>
    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Inventory.Deactivate)]
    [ProducesResponseType<InventoryItemDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryItemDto>> SetStatus(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken) =>
        Ok(await itemService.SetStatusAsync(id, request.IsActive, cancellationToken));

    /// <summary>Kardex del artículo: movimientos ordenados del más reciente al más antiguo.</summary>
    [HttpGet("{id:guid}/movements")]
    [HasPermission(Permissions.Inventory.Read)]
    [ProducesResponseType<PagedResult<InventoryMovementDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<InventoryMovementDto>>> GetMovements(
        Guid id,
        [FromQuery] InventoryMovementQuery query,
        CancellationToken cancellationToken)
    {
        await itemService.GetByIdAsync(id, cancellationToken);
        query.ItemId = id;
        return Ok(await movementService.GetPagedAsync(query, cancellationToken));
    }
}
