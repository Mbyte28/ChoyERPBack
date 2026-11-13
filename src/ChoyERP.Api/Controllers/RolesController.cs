using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Roles;
using ChoyERP.Application.Features.Roles.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Administración de roles y consulta del catálogo de permisos.</summary>
[ApiController]
[Authorize]
[Route("api/roles")]
[Produces("application/json")]
public sealed class RolesController(RoleService roleService) : ControllerBase
{
    /// <summary>Lista todos los roles con sus permisos y cantidad de usuarios.</summary>
    [HttpGet]
    [HasPermission(Permissions.Roles.Read)]
    [ProducesResponseType<IReadOnlyList<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await roleService.GetAllAsync(cancellationToken));

    /// <summary>Obtiene un rol por su identificador.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Roles.Read)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await roleService.GetByIdAsync(id, cancellationToken));

    /// <summary>Crea un rol personalizado.</summary>
    [HttpPost]
    [HasPermission(Permissions.Roles.Manage)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await roleService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
    }

    /// <summary>Actualiza el nombre, la descripción y los permisos de un rol personalizado.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Roles.Manage)]
    [ProducesResponseType<RoleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> Update(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await roleService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Elimina un rol personalizado que no tenga usuarios asignados.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Roles.Manage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await roleService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
