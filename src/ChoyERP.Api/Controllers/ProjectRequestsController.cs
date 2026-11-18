using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Contracting;
using ChoyERP.Application.Features.Contracting.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Solicitudes de proyecto recibidas de los clientes.</summary>
[ApiController]
[Authorize]
[Route("api/project-requests")]
[Produces("application/json")]
public sealed class ProjectRequestsController(ProjectRequestService projectRequestService) : ControllerBase
{
    /// <summary>Lista solicitudes con paginación, búsqueda, filtros y ordenamiento.</summary>
    [HttpGet]
    [HasPermission(Permissions.ProjectRequests.Read)]
    [ProducesResponseType<PagedResult<ProjectRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProjectRequestDto>>> GetAll(
        [FromQuery] ProjectRequestQuery query,
        CancellationToken cancellationToken) =>
        Ok(await projectRequestService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene una solicitud con sus cotizaciones.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.ProjectRequests.Read)]
    [ProducesResponseType<ProjectRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectRequestDto>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await projectRequestService.GetByIdAsync(id, cancellationToken));

    /// <summary>Registra una solicitud de proyecto. El código se genera automáticamente.</summary>
    [HttpPost]
    [HasPermission(Permissions.ProjectRequests.Create)]
    [ProducesResponseType<ProjectRequestDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProjectRequestDto>> Create(
        [FromBody] CreateProjectRequestRequest request,
        CancellationToken cancellationToken)
    {
        var projectRequest = await projectRequestService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = projectRequest.Id }, projectRequest);
    }

    /// <summary>Edita una solicitud mientras está recibida o en evaluación.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.ProjectRequests.Update)]
    [ProducesResponseType<ProjectRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectRequestDto>> Update(
        Guid id,
        [FromBody] UpdateProjectRequestRequest request,
        CancellationToken cancellationToken) =>
        Ok(await projectRequestService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Cambia el estado manualmente. Los estados Quoted y Accepted los asigna el flujo de cotizaciones.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.ProjectRequests.Cancel)]
    [ProducesResponseType<ProjectRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectRequestDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeProjectRequestStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await projectRequestService.ChangeStatusAsync(id, request.Status, cancellationToken));
}
