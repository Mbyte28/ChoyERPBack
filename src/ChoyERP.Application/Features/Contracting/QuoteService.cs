using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Contracting.Dtos;
using ChoyERP.Domain.Entities.Contracting;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Contracting;

public sealed class QuoteService(
    IAppDbContext db,
    IAuditService auditService,
    IDateTimeProvider clock)
{
    public async Task<PagedResult<QuoteDto>> GetPagedAsync(
        QuoteQuery query,
        CancellationToken cancellationToken)
    {
        var quotes = QuoteQueryWithDetails()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            quotes = quotes.Where(q =>
                q.Code.ToLower().Contains(search)
                || q.ProjectRequest.Code.ToLower().Contains(search)
                || q.ProjectRequest.Title.ToLower().Contains(search)
                || q.ProjectRequest.Client.Name.ToLower().Contains(search));
        }

        if (query.Status is not null)
        {
            quotes = quotes.Where(q => q.Status == query.Status);
        }

        if (query.ProjectRequestId is not null)
        {
            quotes = quotes.Where(q => q.ProjectRequestId == query.ProjectRequestId);
        }

        if (query.ClientId is not null)
        {
            quotes = quotes.Where(q => q.ProjectRequest.ClientId == query.ClientId);
        }

        quotes = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? quotes.OrderByDescending(q => q.Code)
                : quotes.OrderBy(q => q.Code),
            "version" => query.SortDesc
                ? quotes.OrderByDescending(q => q.Version)
                : quotes.OrderBy(q => q.Version),
            "status" => query.SortDesc
                ? quotes.OrderByDescending(q => q.Status)
                : quotes.OrderBy(q => q.Status),
            "total" => query.SortDesc
                ? quotes.OrderByDescending(q => q.Total)
                : quotes.OrderBy(q => q.Total),
            "validuntil" => query.SortDesc
                ? quotes.OrderByDescending(q => q.ValidUntil)
                : quotes.OrderBy(q => q.ValidUntil),
            _ => query.SortDesc
                ? quotes.OrderByDescending(q => q.CreatedAt).ThenByDescending(q => q.Code)
                : quotes.OrderBy(q => q.CreatedAt).ThenBy(q => q.Code),
        };

        var page = await quotes.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<QuoteDto>(
            page.Items.Select(q => q.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<QuoteDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var quote = await GetQuoteOrThrowAsync(id, cancellationToken);
        return quote.ToDto();
    }

    public async Task<QuoteDto> CreateAsync(
        CreateQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var projectRequest = await db.ProjectRequests
            .Include(r => r.Client)
            .SingleOrDefaultAsync(r => r.Id == request.ProjectRequestId, cancellationToken)
            ?? throw new NotFoundException("Solicitud", request.ProjectRequestId);

        if (ContractingSupport.IsFinal(projectRequest.Status))
        {
            throw new ConflictException(
                "No se puede cotizar una solicitud rechazada, cancelada o ya aceptada.");
        }

        if (!projectRequest.Client.IsActive)
        {
            throw new ConflictException("El cliente de la solicitud está inactivo.");
        }

        if (await db.Quotes.AnyAsync(
                q => q.ProjectRequestId == projectRequest.Id && q.Status == QuoteStatus.Draft,
                cancellationToken))
        {
            throw new ConflictException(
                "Ya existe una cotización en borrador para esta solicitud. " +
                "Edítela o envíela antes de crear otra versión.");
        }

        var quote = new Quote
        {
            Code = await db.Quotes
                .Select(q => q.Code)
                .NextSequentialCodeAsync("COT-", cancellationToken),
            ProjectRequestId = projectRequest.Id,
            Version = (await db.Quotes
                .Where(q => q.ProjectRequestId == projectRequest.Id)
                .MaxAsync(q => (int?)q.Version, cancellationToken) ?? 0) + 1,
            Status = QuoteStatus.Draft,
            Currency = NormalizeCurrency(request.Currency),
            TaxRate = ValidateTaxRate(request.TaxRate),
            ValidUntil = ValidateValidUntil(request.ValidUntil),
            Notes = request.Notes?.Trim(),
        };

        foreach (var item in MapItems(request.Items))
        {
            quote.Items.Add(item);
        }

        quote.RecalculateTotals();

        db.Quotes.Add(quote);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "quotes.created",
            nameof(Quote),
            quote.Id.ToString(),
            details: $"Cotización: {quote.Code} (v{quote.Version}) para {projectRequest.Code}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(quote.Id, cancellationToken);
    }

    public async Task<QuoteDto> UpdateAsync(
        Guid id,
        UpdateQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var quote = await GetQuoteOrThrowAsync(id, cancellationToken);

        EnsureEditable(quote);

        quote.Currency = NormalizeCurrency(request.Currency);
        quote.TaxRate = ValidateTaxRate(request.TaxRate);
        quote.ValidUntil = ValidateValidUntil(request.ValidUntil);
        quote.Notes = request.Notes?.Trim();

        quote.Items.Clear();

        foreach (var item in MapItems(request.Items))
        {
            quote.Items.Add(item);
        }

        quote.RecalculateTotals();
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "quotes.updated",
            nameof(Quote),
            quote.Id.ToString(),
            details: $"Cotización: {quote.Code} (v{quote.Version}). Total: {quote.Total} {quote.Currency}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(quote.Id, cancellationToken);
    }

    public async Task<QuoteDto> SendAsync(
        Guid id,
        SendQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var quote = await GetQuoteOrThrowAsync(id, cancellationToken);

        EnsureEditable(quote);
        EnsureItems(quote);

        if (request.ValidUntil is not null)
        {
            quote.ValidUntil = ValidateValidUntil(request.ValidUntil);
        }

        quote.Status = QuoteStatus.Sent;
        quote.SentAt = clock.UtcNow;

        if (quote.ProjectRequest.Status is ProjectRequestStatus.Received or ProjectRequestStatus.InEvaluation)
        {
            quote.ProjectRequest.Status = ProjectRequestStatus.Quoted;
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "quotes.sent",
            nameof(Quote),
            quote.Id.ToString(),
            details: $"Cotización enviada: {quote.Code} para {quote.ProjectRequest.Code}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(quote.Id, cancellationToken);
    }

    public async Task<QuoteDto> AcceptAsync(Guid id, CancellationToken cancellationToken)
    {
        var quote = await GetQuoteOrThrowAsync(id, cancellationToken);

        if (quote.Status != QuoteStatus.Sent)
        {
            throw new ConflictException(
                "Solo se puede aceptar una cotización enviada. Envíela primero.");
        }

        var today = ContractingSupport.Today(clock);

        if (quote.ValidUntil is not null && quote.ValidUntil < today)
        {
            quote.Status = QuoteStatus.Expired;
            await db.SaveChangesAsync(cancellationToken);

            await auditService.LogAsync(
                "quotes.expired",
                nameof(Quote),
                quote.Id.ToString(),
                details: $"Cotización vencida: {quote.Code}.",
                cancellationToken: cancellationToken);

            throw new ConflictException("La cotización está vencida y no puede aceptarse.");
        }

        if (quote.ProjectRequest.Status is ProjectRequestStatus.Rejected
            or ProjectRequestStatus.Cancelled
            or ProjectRequestStatus.Accepted)
        {
            throw new ConflictException(
                $"La solicitud está en estado '{quote.ProjectRequest.Status}' y no admite una nueva aceptación.");
        }

        var now = clock.UtcNow;

        var siblingQuotes = await db.Quotes
            .Where(q => q.ProjectRequestId == quote.ProjectRequestId && q.Id != quote.Id)
            .ToListAsync(cancellationToken);

        foreach (var sibling in siblingQuotes.Where(s => !ContractingSupport.IsFinal(s.Status)))
        {
            sibling.Status = QuoteStatus.Rejected;
            sibling.RejectedAt = now;
            sibling.DecisionNotes ??= "Reemplazada por la cotización aceptada.";
        }

        quote.Status = QuoteStatus.Accepted;
        quote.AcceptedAt = now;
        quote.ProjectRequest.Status = ProjectRequestStatus.Accepted;

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "quotes.accepted",
            nameof(Quote),
            quote.Id.ToString(),
            details: $"Cotización aceptada: {quote.Code}. Total: {quote.Total} {quote.Currency}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(quote.Id, cancellationToken);
    }

    public async Task<QuoteDto> RejectAsync(
        Guid id,
        RejectQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var quote = await GetQuoteOrThrowAsync(id, cancellationToken);

        if (quote.Status is not (QuoteStatus.Draft or QuoteStatus.Sent))
        {
            throw new ConflictException("Solo se puede rechazar una cotización en borrador o enviada.");
        }

        quote.Status = QuoteStatus.Rejected;
        quote.RejectedAt = clock.UtcNow;
        quote.DecisionNotes = request.Reason?.Trim();

        if (quote.ProjectRequest.Status == ProjectRequestStatus.Quoted
            && !await db.Quotes.AnyAsync(
                q => q.ProjectRequestId == quote.ProjectRequestId
                     && q.Id != quote.Id
                     && (q.Status == QuoteStatus.Draft || q.Status == QuoteStatus.Sent),
                cancellationToken))
        {
            quote.ProjectRequest.Status = ProjectRequestStatus.InEvaluation;
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "quotes.rejected",
            nameof(Quote),
            quote.Id.ToString(),
            details: $"Cotización rechazada: {quote.Code}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(quote.Id, cancellationToken);
    }

    public async Task<QuoteDto> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var quote = await GetQuoteOrThrowAsync(id, cancellationToken);

        if (quote.Status != QuoteStatus.Draft)
        {
            throw new ConflictException("Solo se puede anular una cotización en borrador.");
        }

        quote.Status = QuoteStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "quotes.cancelled",
            nameof(Quote),
            quote.Id.ToString(),
            details: $"Cotización anulada: {quote.Code}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(quote.Id, cancellationToken);
    }

    private IQueryable<Quote> QuoteQueryWithDetails() =>
        db.Quotes
            .Include(q => q.Items)
            .Include(q => q.Contract)
            .Include(q => q.ProjectRequest)
            .ThenInclude(r => r.Client);

    private async Task<Quote> GetQuoteOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await QuoteQueryWithDetails()
            .SingleOrDefaultAsync(q => q.Id == id, cancellationToken)
        ?? throw new NotFoundException("Cotización", id);

    private static void EnsureEditable(Quote quote)
    {
        if (quote.Status != QuoteStatus.Draft)
        {
            throw new ConflictException(
                "Solo se puede editar una cotización en borrador. " +
                "Cree una nueva versión para cambiar el contenido.");
        }

        if (ContractingSupport.IsFinal(quote.ProjectRequest.Status))
        {
            throw new ConflictException(
                "La solicitud asociada está cerrada; no se puede editar la cotización.");
        }
    }

    private static void EnsureItems(Quote quote)
    {
        if (quote.Items.Count == 0)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Items"] = ["La cotización debe tener al menos una línea."],
            });
        }
    }

    private static string NormalizeCurrency(string currency) =>
        string.IsNullOrWhiteSpace(currency) ? "PEN" : currency.Trim().ToUpperInvariant();

    private static decimal ValidateTaxRate(decimal taxRate)
    {
        if (taxRate is < 0 or > 1)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["TaxRate"] = ["La tasa de impuesto debe estar entre 0 y 1."],
            });
        }

        return taxRate;
    }

    private DateOnly? ValidateValidUntil(DateOnly? validUntil)
    {
        if (validUntil is not null && validUntil < ContractingSupport.Today(clock))
        {
            throw new ConflictException("La fecha de vigencia de la cotización no puede ser anterior a hoy.");
        }

        return validUntil;
    }

    private static List<QuoteItem> MapItems(IReadOnlyList<QuoteItemRequest>? items)
    {
        var errors = new Dictionary<string, string[]>();
        var mapped = new List<QuoteItem>(items?.Count ?? 0);

        if (items is null || items.Count == 0)
        {
            errors["Items"] = ["La cotización debe tener al menos una línea."];
        }
        else if (items.Count > 200)
        {
            errors["Items"] = ["La cotización no puede tener más de 200 líneas."];
        }

        for (var index = 0; index < (items?.Count ?? 0); index++)
        {
            var item = items![index];

            if (string.IsNullOrWhiteSpace(item.Description))
            {
                errors[$"Items[{index}].Description"] = ["La descripción de la línea es obligatoria."];
            }

            if (item.Quantity <= 0)
            {
                errors[$"Items[{index}].Quantity"] = ["La cantidad debe ser mayor que cero."];
            }

            if (item.UnitPrice < 0)
            {
                errors[$"Items[{index}].UnitPrice"] = ["El precio unitario no puede ser negativo."];
            }

            mapped.Add(new QuoteItem
            {
                Description = item.Description?.Trim() ?? string.Empty,
                Unit = item.Unit?.Trim(),
                SortOrder = index,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            });
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(errors);
        }

        return mapped;
    }
}
