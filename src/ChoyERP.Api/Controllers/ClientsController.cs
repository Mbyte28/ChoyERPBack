using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Clients;
using ChoyERP.Application.Features.Clients.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Clientes de CHOY y sus direcciones. Los clientes no tienen acceso al ERP.</summary>
[ApiController]
[Authorize]
[Route("api/clients")]
[Produces("application/json")]
public sealed class ClientsController(ClientService clientService) : ControllerBase
{
    /// <summary>Lista clientes con paginación, búsqueda, filtros y ordenamiento.</summary>
    [HttpGet]
    [HasPermission(Permissions.Clients.Read)]
    [ProducesResponseType<PagedResult<ClientDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ClientDto>>> GetAll(
        [FromQuery] ClientQuery query,
        CancellationToken cancellationToken) =>
        Ok(await clientService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene un cliente con sus direcciones.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Clients.Read)]
    [ProducesResponseType<ClientDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await clientService.GetByIdAsync(id, cancellationToken));

    /// <summary>Registra un cliente. Si no se indica código, se genera automáticamente.</summary>
    [HttpPost]
    [HasPermission(Permissions.Clients.Create)]
    [ProducesResponseType<ClientDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClientDto>> Create(
        [FromBody] CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        var client = await clientService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    /// <summary>Actualiza los datos generales de un cliente.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Clients.Update)]
    [ProducesResponseType<ClientDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientDto>> Update(
        Guid id,
        [FromBody] UpdateClientRequest request,
        CancellationToken cancellationToken) =>
        Ok(await clientService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Activa o desactiva un cliente.</summary>
    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Clients.Deactivate)]
    [ProducesResponseType<ClientDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientDto>> SetStatus(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken) =>
        Ok(await clientService.SetStatusAsync(id, request.IsActive, cancellationToken));

    /// <summary>Agrega una dirección al cliente.</summary>
    [HttpPost("{id:guid}/addresses")]
    [HasPermission(Permissions.Clients.Update)]
    [ProducesResponseType<ClientAddressDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ClientAddressDto>> AddAddress(
        Guid id,
        [FromBody] CreateClientAddressRequest request,
        CancellationToken cancellationToken)
    {
        var address = await clientService.AddAddressAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, address);
    }

    /// <summary>Actualiza una dirección del cliente.</summary>
    [HttpPut("{id:guid}/addresses/{addressId:guid}")]
    [HasPermission(Permissions.Clients.Update)]
    [ProducesResponseType<ClientAddressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientAddressDto>> UpdateAddress(
        Guid id,
        Guid addressId,
        [FromBody] UpdateClientAddressRequest request,
        CancellationToken cancellationToken) =>
        Ok(await clientService.UpdateAddressAsync(id, addressId, request, cancellationToken));

    /// <summary>Elimina una dirección del cliente. Si era la principal, se promueve otra.</summary>
    [HttpDelete("{id:guid}/addresses/{addressId:guid}")]
    [HasPermission(Permissions.Clients.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(
        Guid id,
        Guid addressId,
        CancellationToken cancellationToken)
    {
        await clientService.DeleteAddressAsync(id, addressId, cancellationToken);
        return NoContent();
    }
}
