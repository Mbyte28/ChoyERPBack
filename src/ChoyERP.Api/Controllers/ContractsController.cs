using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Contracting;
using ChoyERP.Application.Features.Contracting.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Contratos generados a partir de cotizaciones aceptadas.</summary>
[ApiController]
[Authorize]
[Route("api/contracts")]
[Produces("application/json")]
public sealed class ContractsController(ContractService contractService) : ControllerBase
{
    /// <summary>Lista contratos con paginación, búsqueda, filtros y ordenamiento.</summary>
    [HttpGet]
    [HasPermission(Permissions.Contracts.Read)]
    [ProducesResponseType<PagedResult<ContractDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ContractDto>>> GetAll(
        [FromQuery] ContractQuery query,
        CancellationToken cancellationToken) =>
        Ok(await contractService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene un contrato con su cotización y proyecto.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Contracts.Read)]
    [ProducesResponseType<ContractDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContractDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await contractService.GetByIdAsync(id, cancellationToken));

    /// <summary>Genera un contrato en borrador desde una cotización aceptada.</summary>
    [HttpPost]
    [HasPermission(Permissions.Contracts.Create)]
    [ProducesResponseType<ContractDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ContractDto>> Create(
        [FromBody] CreateContractRequest request,
        CancellationToken cancellationToken)
    {
        var contract = await contractService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = contract.Id }, contract);
    }

    /// <summary>Edita fechas y notas de un contrato en borrador o vigente.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Contracts.Update)]
    [ProducesResponseType<ContractDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> Update(
        Guid id,
        [FromBody] UpdateContractRequest request,
        CancellationToken cancellationToken) =>
        Ok(await contractService.UpdateAsync(id, request, cancellationToken));

    /// <summary>
    /// Cambia el estado del contrato. Al anularlo se cancela el proyecto asociado.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Contracts.Update)]
    [ProducesResponseType<ContractDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContractDto>> ChangeStatus(
        Guid id,
        [FromBody] ChangeContractStatusRequest request,
        CancellationToken cancellationToken) =>
        Ok(await contractService.ChangeStatusAsync(id, request.Status, cancellationToken));
}
