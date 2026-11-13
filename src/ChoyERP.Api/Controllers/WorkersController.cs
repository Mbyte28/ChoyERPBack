using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Workers;
using ChoyERP.Application.Features.Workers.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Personal de CHOY que puede vincularse a una cuenta de acceso.</summary>
[ApiController]
[Authorize]
[Route("api/workers")]
[Produces("application/json")]
public sealed class WorkersController(WorkerService workerService) : ControllerBase
{
    /// <summary>Lista trabajadores con paginación, búsqueda, filtros y ordenamiento.</summary>
    [HttpGet]
    [HasPermission(Permissions.Workers.Read)]
    [ProducesResponseType<PagedResult<WorkerDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<WorkerDto>>> GetAll(
        [FromQuery] WorkerQuery query,
        CancellationToken cancellationToken) =>
        Ok(await workerService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene un trabajador por su identificador.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Workers.Read)]
    [ProducesResponseType<WorkerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkerDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await workerService.GetByIdAsync(id, cancellationToken));

    /// <summary>Registra un trabajador.</summary>
    [HttpPost]
    [HasPermission(Permissions.Workers.Create)]
    [ProducesResponseType<WorkerDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<WorkerDto>> Create(
        [FromBody] CreateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        var worker = await workerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = worker.Id }, worker);
    }

    /// <summary>Actualiza los datos de un trabajador.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Workers.Update)]
    [ProducesResponseType<WorkerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkerDto>> Update(
        Guid id,
        [FromBody] UpdateWorkerRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workerService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Activa o desactiva un trabajador.</summary>
    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Workers.Deactivate)]
    [ProducesResponseType<WorkerDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkerDto>> SetStatus(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken) =>
        Ok(await workerService.SetStatusAsync(id, request.IsActive, cancellationToken));
}
