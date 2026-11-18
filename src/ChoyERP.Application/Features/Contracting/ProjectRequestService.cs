using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Contracting.Dtos;
using ChoyERP.Domain.Entities.Contracting;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Contracting;

public sealed class ProjectRequestService(
    IAppDbContext db,
    IAuditService auditService,
    IDateTimeProvider clock)
{
    public async Task<PagedResult<ProjectRequestDto>> GetPagedAsync(
        ProjectRequestQuery query,
        CancellationToken cancellationToken)
    {
        var requests = db.ProjectRequests
            .AsNoTracking()
            .Include(r => r.Client)
            .Include(r => r.ClientAddress)
            .Include(r => r.ResponsibleWorker)
            .Include(r => r.Quotes)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            requests = requests.Where(r =>
                r.Code.ToLower().Contains(search)
                || r.Title.ToLower().Contains(search)
                || (r.Description != null && r.Description.ToLower().Contains(search))
                || r.Client.Name.ToLower().Contains(search)
                || (r.Client.TaxId != null && r.Client.TaxId.ToLower().Contains(search)));
        }

        if (query.Status is not null)
        {
            requests = requests.Where(r => r.Status == query.Status);
        }

        if (query.ClientId is not null)
        {
            requests = requests.Where(r => r.ClientId == query.ClientId);
        }

        if (query.ProjectType is not null)
        {
            requests = requests.Where(r => r.ProjectType == query.ProjectType);
        }

        if (query.Priority is not null)
        {
            requests = requests.Where(r => r.Priority == query.Priority);
        }

        if (query.ResponsibleWorkerId is not null)
        {
            requests = requests.Where(r => r.ResponsibleWorkerId == query.ResponsibleWorkerId);
        }

        requests = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? requests.OrderByDescending(r => r.Code)
                : requests.OrderBy(r => r.Code),
            "title" => query.SortDesc
                ? requests.OrderByDescending(r => r.Title)
                : requests.OrderBy(r => r.Title),
            "status" => query.SortDesc
                ? requests.OrderByDescending(r => r.Status)
                : requests.OrderBy(r => r.Status),
            "priority" => query.SortDesc
                ? requests.OrderByDescending(r => r.Priority)
                : requests.OrderBy(r => r.Priority),
            "createdat" => query.SortDesc
                ? requests.OrderByDescending(r => r.CreatedAt)
                : requests.OrderBy(r => r.CreatedAt),
            _ => query.SortDesc
                ? requests.OrderByDescending(r => r.RequestedAt).ThenByDescending(r => r.Code)
                : requests.OrderBy(r => r.RequestedAt).ThenBy(r => r.Code),
        };

        var page = await requests.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<ProjectRequestDto>(
            page.Items.Select(r => r.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<ProjectRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var request = await GetRequestOrThrowAsync(id, cancellationToken);
        return request.ToDto();
    }

    public async Task<ProjectRequestDto> CreateAsync(
        CreateProjectRequestRequest request,
        CancellationToken cancellationToken)
    {
        await ContractingSupport.GetActiveClientAsync(db, request.ClientId, cancellationToken);
        await ContractingSupport.EnsureAddressBelongsToClientAsync(
            db, request.ClientId, request.ClientAddressId, cancellationToken);
        await ContractingSupport.EnsureActiveWorkerAsync(
            db, request.ResponsibleWorkerId, cancellationToken);

        var projectRequest = new ProjectRequest
        {
            Code = await db.ProjectRequests
                .Select(r => r.Code)
                .NextSequentialCodeAsync("SOL-", cancellationToken),
            ClientId = request.ClientId,
            ClientAddressId = request.ClientAddressId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            ProjectType = request.ProjectType,
            Priority = request.Priority,
            Status = ProjectRequestStatus.Received,
            RequestedAt = request.RequestedAt ?? ContractingSupport.Today(clock),
            ResponsibleWorkerId = request.ResponsibleWorkerId,
            Notes = request.Notes?.Trim(),
        };

        db.ProjectRequests.Add(projectRequest);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "requests.created",
            nameof(ProjectRequest),
            projectRequest.Id.ToString(),
            details: $"Solicitud: {projectRequest.Code} - {projectRequest.Title}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(projectRequest.Id, cancellationToken);
    }

    public async Task<ProjectRequestDto> UpdateAsync(
        Guid id,
        UpdateProjectRequestRequest request,
        CancellationToken cancellationToken)
    {
        var projectRequest = await GetRequestOrThrowAsync(id, cancellationToken);

        if (projectRequest.Status is not (ProjectRequestStatus.Received or ProjectRequestStatus.InEvaluation))
        {
            throw new ConflictException(
                "Solo se puede editar una solicitud recibida o en evaluación. " +
                "Cree una nueva versión de cotización si necesita ajustar el alcance.");
        }

        await ContractingSupport.GetActiveClientAsync(db, request.ClientId, cancellationToken);
        await ContractingSupport.EnsureAddressBelongsToClientAsync(
            db, request.ClientId, request.ClientAddressId, cancellationToken);
        await ContractingSupport.EnsureActiveWorkerAsync(
            db, request.ResponsibleWorkerId, cancellationToken);

        projectRequest.ClientId = request.ClientId;
        projectRequest.ClientAddressId = request.ClientAddressId;
        projectRequest.Title = request.Title.Trim();
        projectRequest.Description = request.Description?.Trim();
        projectRequest.ProjectType = request.ProjectType;
        projectRequest.Priority = request.Priority;
        projectRequest.RequestedAt = request.RequestedAt ?? projectRequest.RequestedAt;
        projectRequest.ResponsibleWorkerId = request.ResponsibleWorkerId;
        projectRequest.Notes = request.Notes?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "requests.updated",
            nameof(ProjectRequest),
            projectRequest.Id.ToString(),
            details: $"Solicitud: {projectRequest.Code} - {projectRequest.Title}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(projectRequest.Id, cancellationToken);
    }

    public async Task<ProjectRequestDto> ChangeStatusAsync(
        Guid id,
        ProjectRequestStatus status,
        CancellationToken cancellationToken)
    {
        var projectRequest = await GetRequestOrThrowAsync(id, cancellationToken);

        if (projectRequest.Status == status)
        {
            return projectRequest.ToDto();
        }

        if (status is ProjectRequestStatus.Quoted or ProjectRequestStatus.Accepted)
        {
            throw new ConflictException(
                "Los estados 'Quoted' y 'Accepted' se asignan automáticamente al enviar y aceptar cotizaciones.");
        }

        var allowed = (projectRequest.Status, status) switch
        {
            (ProjectRequestStatus.Received, ProjectRequestStatus.InEvaluation) => true,
            (ProjectRequestStatus.InEvaluation, ProjectRequestStatus.Received) => true,
            (ProjectRequestStatus.Quoted, ProjectRequestStatus.InEvaluation) => true,
            (ProjectRequestStatus.Received or ProjectRequestStatus.InEvaluation or ProjectRequestStatus.Quoted,
                ProjectRequestStatus.Rejected) => true,
            (ProjectRequestStatus.Received or ProjectRequestStatus.InEvaluation or ProjectRequestStatus.Quoted,
                ProjectRequestStatus.Cancelled) => true,
            _ => false,
        };

        if (!allowed)
        {
            throw new ConflictException(
                $"No se puede pasar la solicitud de '{projectRequest.Status}' a '{status}'.");
        }

        var blockedByAcceptedQuote = projectRequest.Quotes.Any(q =>
            q.Status == QuoteStatus.Accepted
            && (q.Contract is null || q.Contract.Status != ContractStatus.Cancelled));

        if (blockedByAcceptedQuote)
        {
            throw new ConflictException(
                "La solicitud tiene una cotización aceptada con un contrato vigente o pendiente. " +
                "Cancele el contrato o el proyecto asociado.");
        }

        foreach (var quote in projectRequest.Quotes.Where(q => !ContractingSupport.IsFinal(q.Status)))
        {
            quote.Status = QuoteStatus.Cancelled;
        }

        projectRequest.Status = status;
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "requests.status_changed",
            nameof(ProjectRequest),
            projectRequest.Id.ToString(),
            details: $"Solicitud: {projectRequest.Code} - {projectRequest.Title}. Nuevo estado: {status}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(projectRequest.Id, cancellationToken);
    }

    private async Task<ProjectRequest> GetRequestOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await db.ProjectRequests
            .Include(r => r.Client)
            .Include(r => r.ClientAddress)
            .Include(r => r.ResponsibleWorker)
            .Include(r => r.Quotes)
            .ThenInclude(q => q.Contract)
            .SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
        ?? throw new NotFoundException("Solicitud", id);
}
