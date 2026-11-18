using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Inventory;
using ChoyERP.Application.Features.Inventory.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Entradas, salidas y ajustes de inventario.</summary>
[ApiController]
[Authorize]
[Route("api/inventory/movements")]
[Produces("application/json")]
public sealed class InventoryMovementsController(InventoryMovementService movementService)
    : ControllerBase
{
    /// <summary>
    /// Lista movimientos con paginación, búsqueda, filtros por artículo, tipo,
    /// proyecto y rango de fechas.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Inventory.Read)]
    [ProducesResponseType<PagedResult<InventoryMovementDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PagedResult<InventoryMovementDto>>> GetAll(
        [FromQuery] InventoryMovementQuery query,
        CancellationToken cancellationToken) =>
        Ok(await movementService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene un movimiento con su artículo y proyecto asociado.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Inventory.Read)]
    [ProducesResponseType<InventoryMovementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryMovementDto>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await movementService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Registra un movimiento. En una entrada la cantidad se suma y actualiza el
    /// costo promedio; en una salida se descuenta validando disponibilidad; en un
    /// ajuste la cantidad es la existencia contada resultante.
    /// </summary>
    [HttpPost]
    [HasPermission(Permissions.Inventory.Move)]
    [ProducesResponseType<InventoryMovementDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InventoryMovementDto>> Create(
        [FromBody] CreateInventoryMovementRequest request,
        CancellationToken cancellationToken)
    {
        var movement = await movementService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = movement.Id }, movement);
    }
}
