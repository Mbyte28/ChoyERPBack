using ChoyERP.Api.Authorization;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Contracting;
using ChoyERP.Application.Features.Contracting.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ChoyERP.Api.Controllers;

/// <summary>Cotizaciones comerciales de las solicitudes de proyecto.</summary>
[ApiController]
[Authorize]
[Route("api/quotes")]
[Produces("application/json")]
public sealed class QuotesController(QuoteService quoteService) : ControllerBase
{
    /// <summary>Lista cotizaciones con paginación, búsqueda, filtros y ordenamiento.</summary>
    [HttpGet]
    [HasPermission(Permissions.Quotes.Read)]
    [ProducesResponseType<PagedResult<QuoteDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<QuoteDto>>> GetAll(
        [FromQuery] QuoteQuery query,
        CancellationToken cancellationToken) =>
        Ok(await quoteService.GetPagedAsync(query, cancellationToken));

    /// <summary>Obtiene una cotización con sus líneas y su contrato.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Quotes.Read)]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuoteDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await quoteService.GetByIdAsync(id, cancellationToken));

    /// <summary>Crea una nueva versión de cotización en borrador para una solicitud.</summary>
    [HttpPost]
    [HasPermission(Permissions.Quotes.Create)]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<QuoteDto>> Create(
        [FromBody] CreateQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var quote = await quoteService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = quote.Id }, quote);
    }

    /// <summary>Reemplaza los datos y las líneas de una cotización en borrador.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Quotes.Update)]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<QuoteDto>> Update(
        Guid id,
        [FromBody] UpdateQuoteRequest request,
        CancellationToken cancellationToken) =>
        Ok(await quoteService.UpdateAsync(id, request, cancellationToken));

    /// <summary>Envía la cotización al cliente y marca la solicitud como cotizada.</summary>
    [HttpPost("{id:guid}/send")]
    [HasPermission(Permissions.Quotes.Send)]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuoteDto>> Send(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] SendQuoteRequest? request,
        CancellationToken cancellationToken) =>
        Ok(await quoteService.SendAsync(id, request ?? new SendQuoteRequest(null), cancellationToken));

    /// <summary>Acepta la cotización. Rechaza las demás y marca la solicitud como aceptada.</summary>
    [HttpPost("{id:guid}/accept")]
    [HasPermission(Permissions.Quotes.Decide)]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuoteDto>> Accept(Guid id, CancellationToken cancellationToken) =>
        Ok(await quoteService.AcceptAsync(id, cancellationToken));

    /// <summary>Rechaza la cotización y registra el motivo.</summary>
    [HttpPost("{id:guid}/reject")]
    [HasPermission(Permissions.Quotes.Decide)]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuoteDto>> Reject(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RejectQuoteRequest? request,
        CancellationToken cancellationToken) =>
        Ok(await quoteService.RejectAsync(id, request ?? new RejectQuoteRequest(null), cancellationToken));

    /// <summary>Anula una cotización en borrador.</summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Quotes.Update)]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuoteDto>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Ok(await quoteService.CancelAsync(id, cancellationToken));
}
