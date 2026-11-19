using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Installations;
using ChoyERP.Application.Features.Installations.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Instalaciones derivadas de ventas de equipos o de proyectos.</summary>
[ApiController]
[Authorize]
[Route("api/installations")]
[Produces("application/json")]
public sealed class InstallationsController(InstallationService installationService)
    : ControllerBase
{
    /// <summary>
    /// Lista instalaciones con paginación, búsqueda y filtros por estado,
    /// cliente, venta, proyecto, técnico y rango de fechas programadas.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Installations.Read)]
    [ProducesResponseType<PagedResult<InstallationDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PagedResult<InstallationDto>>> GetAll(
        [FromQuery] InstallationQuery query,
        CancellationToken cancellationToken) =>
        Ok(await installationService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene una instalación con su venta, proyecto y técnico asignado.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Installations.Read)]
    [ProducesResponseType<InstallationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstallationDto>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await installationService.GetByIdAsync(id, cancellationToken));

    /// <summary>
    /// Programa una instalación para una venta confirmada o entregada, o para
    /// un proyecto. Debe indicarse exactamente uno de los dos orígenes.
    /// </summary>
    [HttpPost]
    [HasPermission(Permissions.Installations.Create)]
    [ProducesResponseType<InstallationDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InstallationDto>> Create(
        [FromBody] CreateInstallationRequest request,
        CancellationToken cancellationToken)
    {
        var installation = await installationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = installation.Id }, installation);
    }

    /// <summary>Edita una instalación pendiente, su dirección, fecha y técnico.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Installations.Update)]
    [ProducesResponseType<InstallationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InstallationDto>> Update(
        Guid id,
        [FromBody] UpdateInstallationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await installationService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Marca el inicio de los trabajos de instalación.</summary>
    [HttpPost("{id:guid}/start")]
    [HasPermission(Permissions.Installations.Complete)]
    [ProducesResponseType<InstallationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InstallationDto>> Start(
        Guid id,
        [FromBody] StartInstallationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await installationService.StartAsync(id, request, cancellationToken));

    /// <summary>
    /// Completa la instalación. Si la venta asociada ya fue entregada, la venta
    /// se cierra automáticamente.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [HasPermission(Permissions.Installations.Complete)]
    [ProducesResponseType<InstallationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InstallationDto>> Complete(
        Guid id,
        [FromBody] CompleteInstallationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await installationService.CompleteAsync(id, request, cancellationToken));

    /// <summary>Anula una instalación pendiente indicando el motivo.</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Installations.Cancel)]
    [ProducesResponseType<InstallationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<InstallationDto>> Cancel(
        Guid id,
        [FromBody] CancelInstallationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await installationService.CancelAsync(id, request.Reason, cancellationToken));
}
