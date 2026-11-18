using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Contracting.Dtos;
using ChoyERP.Domain.Entities.Contracting;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Contracting;

public sealed class ProjectService(
    IAppDbContext db,
    IAuditService auditService,
    IDateTimeProvider clock)
{
    public async Task<PagedResult<ProjectDto>> GetPagedAsync(
        ProjectQuery query,
        CancellationToken cancellationToken)
    {
        var projects = ProjectQueryWithDetails().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            projects = projects.Where(p =>
                p.Code.ToLower().Contains(search)
                || p.Name.ToLower().Contains(search)
                || (p.Location != null && p.Location.ToLower().Contains(search))
                || p.Client.Name.ToLower().Contains(search)
                || p.Contract.Code.ToLower().Contains(search));
        }

        if (query.Status is not null)
        {
            projects = projects.Where(p => p.Status == query.Status);
        }

        if (query.ClientId is not null)
        {
            projects = projects.Where(p => p.ClientId == query.ClientId);
        }

        if (query.ContractId is not null)
        {
            projects = projects.Where(p => p.ContractId == query.ContractId);
        }

        if (query.ResponsibleWorkerId is not null)
        {
            projects = projects.Where(p => p.ResponsibleWorkerId == query.ResponsibleWorkerId);
        }

        projects = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? projects.OrderByDescending(p => p.Code)
                : projects.OrderBy(p => p.Code),
            "name" => query.SortDesc
                ? projects.OrderByDescending(p => p.Name)
                : projects.OrderBy(p => p.Name),
            "status" => query.SortDesc
                ? projects.OrderByDescending(p => p.Status)
                : projects.OrderBy(p => p.Status),
            "progress" => query.SortDesc
                ? projects.OrderByDescending(p => p.ProgressPercent)
                : projects.OrderBy(p => p.ProgressPercent),
            "startdate" => query.SortDesc
                ? projects.OrderByDescending(p => p.StartDate)
                : projects.OrderBy(p => p.StartDate),
            _ => query.SortDesc
                ? projects.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Code)
                : projects.OrderBy(p => p.CreatedAt).ThenBy(p => p.Code),
        };

        var page = await projects.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<ProjectDto>(
            page.Items.Select(p => p.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<ProjectDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await GetProjectOrThrowAsync(id, cancellationToken);
        return project.ToDto();
    }

    public async Task<ProjectDto> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var contract = await db.Contracts
            .Include(c => c.Client)
            .Include(c => c.Quote)
            .ThenInclude(q => q.ProjectRequest)
            .ThenInclude(r => r.ClientAddress)
            .SingleOrDefaultAsync(c => c.Id == request.ContractId, cancellationToken)
            ?? throw new NotFoundException("Contrato", request.ContractId);

        if (contract.Status != ContractStatus.Active)
        {
            throw new ConflictException(
                "El contrato debe estar vigente para crear el proyecto.");
        }

        if (!contract.Client.IsActive)
        {
            throw new ConflictException("El cliente está inactivo y no puede iniciar un proyecto.");
        }

        if (await db.Projects.AnyAsync(p => p.ContractId == contract.Id, cancellationToken))
        {
            throw new ConflictException("El contrato ya tiene un proyecto asociado.");
        }

        await ContractingSupport.EnsureActiveWorkerAsync(
            db, request.ResponsibleWorkerId, cancellationToken);

        ValidateDateRange(request.StartDate, request.ExpectedEndDate);

        var projectRequest = contract.Quote.ProjectRequest;
        var name = string.IsNullOrWhiteSpace(request.Name)
            ? projectRequest.Title
            : request.Name.Trim();

        var project = new Project
        {
            Code = await db.Projects
                .Select(p => p.Code)
                .NextSequentialCodeAsync("PRY-", cancellationToken),
            ContractId = contract.Id,
            ClientId = contract.ClientId,
            ClientAddressId = projectRequest.ClientAddressId,
            Location = projectRequest.ClientAddress is null
                ? null
                : ContractingMappings.FormatAddress(projectRequest.ClientAddress),
            Name = name,
            Description = request.Description?.Trim(),
            Status = ProjectStatus.Planned,
            ProgressPercent = 0,
            StartDate = request.StartDate ?? ContractingSupport.Today(clock),
            ExpectedEndDate = request.ExpectedEndDate,
            ResponsibleWorkerId = request.ResponsibleWorkerId,
            Notes = request.Notes?.Trim(),
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "projects.created",
            nameof(Project),
            project.Id.ToString(),
            details: $"Proyecto: {project.Code} - {project.Name} desde contrato {contract.Code}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectDto> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await GetProjectOrThrowAsync(id, cancellationToken);

        if (ContractingSupport.IsFinal(project.Status))
        {
            throw new ConflictException(
                "Un proyecto completado o cancelado no se puede editar.");
        }

        await ContractingSupport.EnsureActiveWorkerAsync(
            db, request.ResponsibleWorkerId, cancellationToken);

        ValidateDateRange(request.StartDate, request.ExpectedEndDate);

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.StartDate = request.StartDate;
        project.ExpectedEndDate = request.ExpectedEndDate;
        project.ResponsibleWorkerId = request.ResponsibleWorkerId;
        project.Location = request.Location?.Trim();
        project.Notes = request.Notes?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "projects.updated",
            nameof(Project),
            project.Id.ToString(),
            details: $"Proyecto: {project.Code} - {project.Name}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectDto> UpdateProgressAsync(
        Guid id,
        UpdateProjectProgressRequest request,
        CancellationToken cancellationToken)
    {
        var project = await GetProjectOrThrowAsync(id, cancellationToken);

        if (ContractingSupport.IsFinal(project.Status))
        {
            throw new ConflictException(
                "Un proyecto completado o cancelado no admite cambios de avance.");
        }

        if (request.ProgressPercent is < 0 or > 100)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["ProgressPercent"] = ["El avance debe estar entre 0 y 100."],
            });
        }

        project.ProgressPercent = request.ProgressPercent;
        project.Notes = request.Notes?.Trim() ?? project.Notes;

        var contractCompleted = false;

        if (request.ProgressPercent == 100)
        {
            contractCompleted = Complete(project);
        }
        else if (request.ProgressPercent > 0 && project.Status == ProjectStatus.Planned)
        {
            project.Status = ProjectStatus.InProgress;
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            contractCompleted ? "projects.completed" : "projects.progress_updated",
            nameof(Project),
            project.Id.ToString(),
            details: $"Proyecto: {project.Code}. Avance: {project.ProgressPercent}%.",
            cancellationToken: cancellationToken);

        if (contractCompleted)
        {
            await auditService.LogAsync(
                "contracts.completed",
                nameof(Contract),
                project.ContractId.ToString(),
                details: $"Contrato {project.Contract.Code} completado al cerrar el proyecto {project.Code}.",
                cancellationToken: cancellationToken);
        }

        return await GetByIdAsync(project.Id, cancellationToken);
    }

    public async Task<ProjectDto> ChangeStatusAsync(
        Guid id,
        ProjectStatus status,
        CancellationToken cancellationToken)
    {
        var project = await GetProjectOrThrowAsync(id, cancellationToken);

        if (project.Status == status)
        {
            return project.ToDto();
        }

        if (ContractingSupport.IsFinal(project.Status))
        {
            throw new ConflictException(
                $"El proyecto está en estado final '{project.Status}' y no admite cambios.");
        }

        var contractCompleted = false;

        switch (status)
        {
            case ProjectStatus.InProgress
                when project.Status is ProjectStatus.Planned or ProjectStatus.Paused:
                project.Status = ProjectStatus.InProgress;
                break;

            case ProjectStatus.Paused when project.Status == ProjectStatus.InProgress:
                project.Status = ProjectStatus.Paused;
                break;

            case ProjectStatus.Completed
                when project.Status is ProjectStatus.Planned or ProjectStatus.InProgress or ProjectStatus.Paused:
                contractCompleted = Complete(project);
                break;

            case ProjectStatus.Cancelled
                when project.Status is ProjectStatus.Planned or ProjectStatus.InProgress or ProjectStatus.Paused:
                project.Status = ProjectStatus.Cancelled;
                break;

            default:
                throw new ConflictException(
                    $"No se puede pasar el proyecto de '{project.Status}' a '{status}'.");
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "projects.status_changed",
            nameof(Project),
            project.Id.ToString(),
            details: $"Proyecto: {project.Code}. Nuevo estado: {status}.",
            cancellationToken: cancellationToken);

        if (contractCompleted)
        {
            await auditService.LogAsync(
                "contracts.completed",
                nameof(Contract),
                project.ContractId.ToString(),
                details: $"Contrato {project.Contract.Code} completado al cerrar el proyecto {project.Code}.",
                cancellationToken: cancellationToken);
        }

        return await GetByIdAsync(project.Id, cancellationToken);
    }

    /// <summary>
    /// Cierra el proyecto y, si el contrato sigue vigente, lo completa.
    /// Devuelve true cuando el contrato se completó automáticamente.
    /// </summary>
    private bool Complete(Project project)
    {
        project.Status = ProjectStatus.Completed;
        project.ProgressPercent = 100;
        project.ActualEndDate = ContractingSupport.Today(clock);

        if (project.Contract.Status != ContractStatus.Active)
        {
            return false;
        }

        project.Contract.Status = ContractStatus.Completed;
        return true;
    }

    private IQueryable<Project> ProjectQueryWithDetails() =>
        db.Projects
            .Include(p => p.Client)
            .Include(p => p.ClientAddress)
            .Include(p => p.ResponsibleWorker)
            .Include(p => p.Contract)
            .ThenInclude(c => c.Quote);

    private async Task<Project> GetProjectOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await ProjectQueryWithDetails()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken)
        ?? throw new NotFoundException("Proyecto", id);

    private static void ValidateDateRange(DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate is not null && endDate is not null && endDate < startDate)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["ExpectedEndDate"] = ["La fecha de fin no puede ser anterior a la fecha de inicio."],
            });
        }
    }
}
