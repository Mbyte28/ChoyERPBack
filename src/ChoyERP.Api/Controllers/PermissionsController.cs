using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Roles;
using ChoyERP.Application.Features.Roles.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChoyERP.Api.Controllers;

/// <summary>Catálogo de permisos disponible para asignar a roles.</summary>
[ApiController]
[Authorize]
[Route("api/permissions")]
[Produces("application/json")]
public sealed class PermissionsController(PermissionService permissionService) : ControllerBase
{
    /// <summary>Lista todos los permisos agrupables por módulo.</summary>
    [HttpGet]
    [HasPermission(Permissions.Roles.Read)]
    [ProducesResponseType<IReadOnlyList<PermissionDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await permissionService.GetAllAsync(cancellationToken));
}
