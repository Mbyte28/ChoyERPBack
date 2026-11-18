using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Contracting;
using ChoyERP.Application.Features.Contracting.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Proyectos de ejecución derivados de contratos vigentes.</summary>
[ApiController]
[Authorize]
[Route("api/projects")]
[Produces("application/json")]
public sealed class ProjectsController(ProjectService projectService) : ControllerBase
{
    /// <summary>Lista proyectos con paginación, búsqueda, filtros y ordenamiento.</summary>
    [HttpGet]
    [HasPermission(Permissions.Projects.Read)]
    [ProducesResponseType<PagedResult<ProjectDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ProjectDto>>> GetAll(
        [FromQuery] ProjectQuery query,
        CancellationToken cancellationToken) =>
        Ok(await projectService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene un proyecto con su contrato, ubicación y responsable.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Projects.Read)]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await projectService.GetByIdAsync(id, cancellationToken));

    /// <summary>Crea el proyecto de un contrato vigente. El código se genera automáticamente.</summary>
    [HttpPost]
    [HasPermission(Permissions.Projects.Create)]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProjectDto>> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projectService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    /// <summary>Edita los datos generales de un proyecto no finalizado.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Projects.Update)]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDto>> Update(
        Guid id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken) =>
        Ok(await projectService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Actualiza el avance. Al llegar a 100 % el proyecto se completa y el contrato se cierra.
    /// </summary>
    [HttpPatch("{id:guid}/progress")]
    [HasPermission(Permissions.Projects.Update)]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProjectDto>> UpdateProgress(
        Guid id,
        [FromBody] UpdateProjectProgressRequest request,
        CancellationToken cancellationToken) =>
        Ok(await projectService.UpdateProgressAsync(id, request, cancellationToken));

    /// <summary>Cambia el estado del proyecto respetando las transiciones permitidas.</summary>
    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Projects.Update)]
    [ProducesResponseType<ProjectDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeProjectStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await projectService.ChangeStatusAsync(id, request.Status, cancellationToken));
}
