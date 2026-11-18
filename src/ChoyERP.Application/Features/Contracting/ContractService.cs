using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Contracting.Dtos;
using ChoyERP.Domain.Entities.Contracting;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Contracting;

public sealed class ContractService(
    IAppDbContext db,
    IAuditService auditService,
    IDateTimeProvider clock)
{
    public async Task<PagedResult<ContractDto>> GetPagedAsync(
        ContractQuery query,
        CancellationToken cancellationToken)
    {
        var contracts = ContractQueryWithDetails().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            contracts = contracts.Where(c =>
                c.Code.ToLower().Contains(search)
                || c.Quote.Code.ToLower().Contains(search)
                || c.Quote.ProjectRequest.Code.ToLower().Contains(search)
                || c.Quote.ProjectRequest.Title.ToLower().Contains(search)
                || c.Client.Name.ToLower().Contains(search));
        }

        if (query.Status is not null)
        {
            contracts = contracts.Where(c => c.Status == query.Status);
        }

        if (query.ClientId is not null)
        {
            contracts = contracts.Where(c => c.ClientId == query.ClientId);
        }

        if (query.QuoteId is not null)
        {
            contracts = contracts.Where(c => c.QuoteId == query.QuoteId);
        }

        contracts = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? contracts.OrderByDescending(c => c.Code)
                : contracts.OrderBy(c => c.Code),
            "status" => query.SortDesc
                ? contracts.OrderByDescending(c => c.Status)
                : contracts.OrderBy(c => c.Status),
            "amount" => query.SortDesc
                ? contracts.OrderByDescending(c => c.Amount)
                : contracts.OrderBy(c => c.Amount),
            "startdate" => query.SortDesc
                ? contracts.OrderByDescending(c => c.StartDate)
                : contracts.OrderBy(c => c.StartDate),
            _ => query.SortDesc
                ? contracts.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Code)
                : contracts.OrderBy(c => c.CreatedAt).ThenBy(c => c.Code),
        };

        var page = await contracts.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<ContractDto>(
            page.Items.Select(c => c.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<ContractDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var contract = await GetContractOrThrowAsync(id, cancellationToken);
        return contract.ToDto();
    }

    public async Task<ContractDto> CreateAsync(
        CreateContractRequest request,
        CancellationToken cancellationToken)
    {
        var quote = await db.Quotes
            .Include(q => q.ProjectRequest)
            .ThenInclude(r => r.Client)
            .SingleOrDefaultAsync(q => q.Id == request.QuoteId, cancellationToken)
            ?? throw new NotFoundException("Cotización", request.QuoteId);

        if (quote.Status != QuoteStatus.Accepted)
        {
            throw new ConflictException(
                "Solo se puede crear un contrato a partir de una cotización aceptada.");
        }

        if (quote.ProjectRequest.Status != ProjectRequestStatus.Accepted)
        {
            throw new ConflictException("La solicitud asociada no está aceptada.");
        }

        if (!quote.ProjectRequest.Client.IsActive)
        {
            throw new ConflictException("El cliente está inactivo y no puede firmar un contrato.");
        }

        if (await db.Contracts.AnyAsync(c => c.QuoteId == quote.Id, cancellationToken))
        {
            throw new ConflictException("La cotización ya tiene un contrato asociado.");
        }

        ValidateDateRange(request.StartDate, request.EndDate);

        var contract = new Contract
        {
            Code = await db.Contracts
                .Select(c => c.Code)
                .NextSequentialCodeAsync("CTR-", cancellationToken),
            QuoteId = quote.Id,
            ClientId = quote.ProjectRequest.ClientId,
            Status = ContractStatus.Draft,
            Amount = quote.Total,
            Currency = quote.Currency,
            SignedAt = request.SignedAt,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Notes = request.Notes?.Trim(),
        };

        db.Contracts.Add(contract);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "contracts.created",
            nameof(Contract),
            contract.Id.ToString(),
            details: $"Contrato: {contract.Code} desde cotización {quote.Code}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(contract.Id, cancellationToken);
    }

    public async Task<ContractDto> UpdateAsync(
        Guid id,
        UpdateContractRequest request,
        CancellationToken cancellationToken)
    {
        var contract = await GetContractOrThrowAsync(id, cancellationToken);

        if (contract.Status is not (ContractStatus.Draft or ContractStatus.Active))
        {
            throw new ConflictException(
                "Solo se puede editar un contrato en borrador o vigente.");
        }

        ValidateDateRange(request.StartDate, request.EndDate);

        contract.SignedAt = request.SignedAt;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.Notes = request.Notes?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "contracts.updated",
            nameof(Contract),
            contract.Id.ToString(),
            details: $"Contrato: {contract.Code}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(contract.Id, cancellationToken);
    }

    public async Task<ContractDto> ChangeStatusAsync(
        Guid id,
        ContractStatus status,
        CancellationToken cancellationToken)
    {
        var contract = await GetContractOrThrowAsync(id, cancellationToken);

        if (contract.Status == status)
        {
            return contract.ToDto();
        }

        if (ContractingSupport.IsFinal(contract.Status))
        {
            throw new ConflictException(
                $"El contrato está en estado final '{contract.Status}' y no admite cambios.");
        }

        var projectCancelled = false;

        switch (status)
        {
            case ContractStatus.Active when contract.Status == ContractStatus.Draft:
                contract.Status = ContractStatus.Active;
                contract.SignedAt ??= ContractingSupport.Today(clock);
                break;

            case ContractStatus.Completed when contract.Status == ContractStatus.Active:
                if (contract.Project is not null && contract.Project.Status != ProjectStatus.Completed)
                {
                    throw new ConflictException(
                        "El proyecto asociado no está completado; no se puede cerrar el contrato.");
                }

                contract.Status = ContractStatus.Completed;
                break;

            case ContractStatus.Cancelled
                when contract.Status is ContractStatus.Draft or ContractStatus.Active:

                if (contract.Project is { Status: ProjectStatus.Completed })
                {
                    throw new ConflictException(
                        "El proyecto ya está completado; no se puede anular el contrato.");
                }

                contract.Status = ContractStatus.Cancelled;

                if (contract.Project is not null
                    && !ContractingSupport.IsFinal(contract.Project.Status))
                {
                    contract.Project.Status = ProjectStatus.Cancelled;
                    projectCancelled = true;
                }

                if (contract.Quote.ProjectRequest.Status == ProjectRequestStatus.Accepted)
                {
                    contract.Quote.ProjectRequest.Status = ProjectRequestStatus.InEvaluation;
                }

                // La cotización aceptada deja de serlo al anularse su contrato,
                // para conservar la invariante de una sola cotización aceptada por solicitud.
                if (contract.Quote.Status == QuoteStatus.Accepted)
                {
                    contract.Quote.Status = QuoteStatus.Cancelled;
                    contract.Quote.DecisionNotes = $"Contrato {contract.Code} anulado.";
                }

                break;

            default:
                throw new ConflictException(
                    $"No se puede pasar el contrato de '{contract.Status}' a '{status}'.");
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "contracts.status_changed",
            nameof(Contract),
            contract.Id.ToString(),
            details: $"Contrato: {contract.Code}. Nuevo estado: {status}.",
            cancellationToken: cancellationToken);

        if (projectCancelled && contract.Project is not null)
        {
            await auditService.LogAsync(
                "projects.cancelled",
                nameof(Project),
                contract.Project.Id.ToString(),
                details: $"Proyecto {contract.Project.Code} cancelado por anulación del contrato {contract.Code}.",
                cancellationToken: cancellationToken);
        }

        return await GetByIdAsync(contract.Id, cancellationToken);
    }

    private IQueryable<Contract> ContractQueryWithDetails() =>
        db.Contracts
            .Include(c => c.Client)
            .Include(c => c.Project)
            .Include(c => c.Quote)
            .ThenInclude(q => q.ProjectRequest);

    private async Task<Contract> GetContractOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await ContractQueryWithDetails()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
        ?? throw new NotFoundException("Contrato", id);

    private static void ValidateDateRange(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate is not null && endDate is not null && endDate < startDate)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["EndDate"] = ["La fecha de fin no puede ser anterior a la fecha de inicio."],
            });
        }
    }
}
