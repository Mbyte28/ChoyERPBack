using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Equipment;
using ChoyERP.Application.Features.Equipment.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Equipos instalados en clientes y seguimiento de su estado operativo.</summary>
[ApiController]
[Authorize]
[Route("api/equipment")]
[Produces("application/json")]
public sealed class EquipmentController(EquipmentService equipmentService) : ControllerBase
{
    /// <summary>
    /// Lista equipos instalados con paginación, búsqueda y filtros por estado,
    /// cliente, instalación, artículo y vigencia de garantía.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Equipment.Read)]
    [ProducesResponseType<PagedResult<EquipmentDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PagedResult<EquipmentDto>>> GetAll(
        [FromQuery] EquipmentQuery query,
        CancellationToken cancellationToken) =>
        Ok(await equipmentService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene un equipo con su cliente, instalación y artículo de origen.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Equipment.Read)]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EquipmentDto>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await equipmentService.GetByIdAsync(id, cancellationToken));

    /// <summary>Historial de estados del equipo, del más reciente al más antiguo.</summary>
    [HttpGet("{id:guid}/history")]
    [HasPermission(Permissions.Equipment.Read)]
    [ProducesResponseType<IReadOnlyList<EquipmentStatusChangeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EquipmentStatusChangeDto>>> GetHistory(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await equipmentService.GetHistoryAsync(id, cancellationToken));

    /// <summary>
    /// Registra un equipo sobre una instalación completada. El código se genera
    /// automáticamente con el prefijo EQP- y la fecha de instalación se toma de
    /// la instalación si no se envía.
    /// </summary>
    [HttpPost]
    [HasPermission(Permissions.Equipment.Create)]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EquipmentDto>> Create(
        [FromBody] CreateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var equipment = await equipmentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = equipment.Id }, equipment);
    }

    /// <summary>Edita la ficha del equipo: nombre, serie, marca, modelo, ubicación y garantía.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Equipment.Update)]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EquipmentDto>> Update(
        Guid id,
        [FromBody] UpdateEquipmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await equipmentService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Cambia el estado operativo del equipo y registra el cambio en su historial.
    /// Un equipo dado de baja ya no admite cambios.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [HasPermission(Permissions.Equipment.ChangeStatus)]
    [ProducesResponseType<EquipmentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EquipmentDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeEquipmentStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await equipmentService.ChangeStatusAsync(id, request, cancellationToken));
}
