using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Sales;
using ChoyERP.Application.Features.Sales.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Ventas de equipos a clientes y su ciclo de entrega.</summary>
[ApiController]
[Authorize]
[Route("api/sales")]
[Produces("application/json")]
public sealed class SalesController(SaleService saleService) : ControllerBase
{
    /// <summary>
    /// Lista ventas con paginación, búsqueda, filtros por estado, cliente,
    /// proyecto y rango de fechas de venta.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Sales.Read)]
    [ProducesResponseType<PagedResult<SaleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PagedResult<SaleDto>>> GetAll(
        [FromQuery] SaleQuery query,
        CancellationToken cancellationToken) =>
        Ok(await saleService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene una venta con sus líneas, importes e instalación asociada.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Sales.Read)]
    [ProducesResponseType<SaleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleDto>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await saleService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Registra una venta en borrador. No descuenta stock; para eso debe
    /// confirmarse. El código se genera automáticamente con el prefijo VEN-.
    /// </summary>
    [HttpPost]
    [HasPermission(Permissions.Sales.Create)]
    [ProducesResponseType<SaleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SaleDto>> Create(
        [FromBody] CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        var sale = await saleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, sale);
    }

    /// <summary>Edita una venta en borrador y recalcula sus importes.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Sales.Update)]
    [ProducesResponseType<SaleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SaleDto>> Update(
        Guid id,
        [FromBody] UpdateSaleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await saleService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Confirma la venta, valida la disponibilidad y descuenta el stock con
    /// movimientos de salida asociados a la venta.
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [HasPermission(Permissions.Sales.Confirm)]
    [ProducesResponseType<SaleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleDto>> Confirm(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await saleService.ConfirmAsync(id, cancellationToken));

    /// <summary>Registra la entrega física de los equipos vendidos.</summary>
    [HttpPost("{id:guid}/deliver")]
    [HasPermission(Permissions.Sales.Deliver)]
    [ProducesResponseType<SaleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SaleDto>> Deliver(
        Guid id,
        [FromBody] DeliverSaleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await saleService.DeliverAsync(id, request, cancellationToken));

    /// <summary>
    /// Cierra la venta entregada. Si tiene una instalación pendiente, primero
    /// debe completarse.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [HasPermission(Permissions.Sales.Complete)]
    [ProducesResponseType<SaleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleDto>> Complete(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await saleService.CompleteAsync(id, cancellationToken));

    /// <summary>Anula una venta en borrador indicando el motivo.</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Sales.Cancel)]
    [ProducesResponseType<SaleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SaleDto>> Cancel(
        Guid id,
        [FromBody] CancelSaleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await saleService.CancelAsync(id, request.Reason, cancellationToken));
}
