using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Auth;
using ChoyERP.Application.Features.Auth.Dtos;
using ChoyERP.Application.Features.Users;
using ChoyERP.Application.Features.Users.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Gestión de cuentas de acceso y consulta del usuario autenticado.</summary>
[ApiController]
[Authorize]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController(UserService userService, AuthService authService) : ControllerBase
{
    /// <summary>Devuelve la cuenta autenticada con sus roles y permisos efectivos.</summary>
    [HttpGet("me")]
    [ProducesResponseType<AuthenticatedUserDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticatedUserDto>> GetMe(CancellationToken cancellationToken) =>
        Ok(await authService.GetCurrentUserAsync(cancellationToken));

    /// <summary>Lista usuarios con paginación, búsqueda, filtros y ordenamiento.</summary>
    [HttpGet]
    [HasPermission(Permissions.Users.Read)]
    [ProducesResponseType<PagedResult<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
        [FromQuery] UserQuery query,
        CancellationToken cancellationToken) =>
        Ok(await userService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene un usuario por su identificador.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Users.Read)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await userService.GetByIdAsync(id, cancellationToken));

    /// <summary>Crea una cuenta de acceso y le asigna roles.</summary>
    [HttpPost]
    [HasPermission(Permissions.Users.Create)]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>Actualiza los datos, el trabajador vinculado y los roles de un usuario.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Users.Update)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Activa o desactiva una cuenta. Al desactivarla se revocan sus sesiones.</summary>
    [HttpPatch("{id:guid}/status")]
    [HasPermission(Permissions.Users.Deactivate)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> SetStatus(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken) =>
        Ok(await userService.SetStatusAsync(id, request.IsActive, cancellationToken));

    /// <summary>Restablece la contraseña de un usuario y revoca sus sesiones activas.</summary>
    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(Permissions.Users.ResetPassword)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await userService.ResetPasswordAsync(id, request.NewPassword, cancellationToken);
        return NoContent();
    }
}
