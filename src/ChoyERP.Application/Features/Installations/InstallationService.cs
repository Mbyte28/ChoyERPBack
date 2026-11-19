using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Installations.Dtos;
using ChoyERP.Application.Features.Sales;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Sales;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Installations;

public sealed class InstallationService(
    IAppDbContext db,
    IAuditService auditService,
    IDateTimeProvider clock)
{
    public async Task<PagedResult<InstallationDto>> GetPagedAsync(
        InstallationQuery query,
        CancellationToken cancellationToken)
    {
        if (query.FromDate is not null
            && query.ToDate is not null
            && query.ToDate < query.FromDate)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["FromDate"] = ["La fecha inicial no puede ser posterior a la fecha final."],
            });
        }

        var installations = InstallationQueryWithDetails().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            installations = installations.Where(i =>
                i.Code.ToLower().Contains(search)
                || i.Name.ToLower().Contains(search)
                || i.Client.Name.ToLower().Contains(search)
                || (i.Sale != null && i.Sale.Code.ToLower().Contains(search))
                || (i.Project != null && i.Project.Code.ToLower().Contains(search))
                || (i.Location != null && i.Location.ToLower().Contains(search)));
        }

        if (query.Status is not null)
        {
            installations = installations.Where(i => i.Status == query.Status);
        }

        if (query.ClientId is not null)
        {
            installations = installations.Where(i => i.ClientId == query.ClientId);
        }

        if (query.SaleId is not null)
        {
            installations = installations.Where(i => i.SaleId == query.SaleId);
        }

        if (query.ProjectId is not null)
        {
            installations = installations.Where(i => i.ProjectId == query.ProjectId);
        }

        if (query.TechnicianWorkerId is not null)
        {
            installations = installations.Where(
                i => i.TechnicianWorkerId == query.TechnicianWorkerId);
        }

        if (query.FromDate is not null)
        {
            installations = installations.Where(
                i => i.ScheduledDate != null && i.ScheduledDate >= query.FromDate);
        }

        if (query.ToDate is not null)
        {
            installations = installations.Where(
                i => i.ScheduledDate != null && i.ScheduledDate <= query.ToDate);
        }

        installations = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? installations.OrderByDescending(i => i.Code)
                : installations.OrderBy(i => i.Code),
            "status" => query.SortDesc
                ? installations.OrderByDescending(i => i.Status)
                : installations.OrderBy(i => i.Status),
            "clientname" => query.SortDesc
                ? installations.OrderByDescending(i => i.Client.Name)
                : installations.OrderBy(i => i.Client.Name),
            "scheduleddate" => query.SortDesc
                ? installations.OrderByDescending(i => i.ScheduledDate).ThenByDescending(i => i.Code)
                : installations.OrderBy(i => i.ScheduledDate).ThenBy(i => i.Code),
            "createdat" => query.SortDesc
                ? installations.OrderByDescending(i => i.CreatedAt)
                : installations.OrderBy(i => i.CreatedAt),
            _ => query.SortDesc
                ? installations.OrderByDescending(i => i.ScheduledDate).ThenByDescending(i => i.Code)
                : installations.OrderBy(i => i.ScheduledDate).ThenBy(i => i.Code),
        };

        var page = await installations.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<InstallationDto>(
            page.Items.Select(i => i.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<InstallationDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var installation = await GetInstallationOrThrowAsync(id, cancellationToken);
        return installation.ToDto();
    }

    public async Task<InstallationDto> CreateAsync(
        CreateInstallationRequest request,
        CancellationToken cancellationToken)
    {
        if ((request.SaleId is null) == (request.ProjectId is null))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["SaleId"] =
                [
                    "La instalación debe asociarse a una venta o a un proyecto, no a ambos.",
                ],
            });
        }

        Client client;
        Sale? sale = null;
        Project? project = null;
        string defaultName;
        string? defaultLocation = null;

        if (request.SaleId is not null)
        {
            sale = await db.Sales
                .Include(s => s.Client)
                .Include(s => s.Project)
                .ThenInclude(p => p!.ClientAddress)
                .SingleOrDefaultAsync(s => s.Id == request.SaleId, cancellationToken)
                ?? throw new NotFoundException("Venta", request.SaleId.Value);

            if (sale.Status is not (SaleStatus.Confirmed or SaleStatus.Delivered))
            {
                throw new ConflictException(
                    "Solo se puede programar la instalación de una venta confirmada o entregada.");
            }

            if (await db.Installations.AnyAsync(i => i.SaleId == sale.Id, cancellationToken))
            {
                throw new ConflictException("La venta ya tiene una instalación asociada.");
            }

            client = sale.Client;
            defaultName = $"Instalación de venta {sale.Code}";
            defaultLocation = sale.Project?.Location
                ?? sale.Project?.ClientAddress?.ToSingleLine();
        }
        else
        {
            project = await db.Projects
                .Include(p => p.Client)
                .Include(p => p.ClientAddress)
                .SingleOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
                ?? throw new NotFoundException("Proyecto", request.ProjectId!.Value);

            if (project.Status == ProjectStatus.Cancelled)
            {
                throw new ConflictException("El proyecto está cancelado y no admite instalaciones.");
            }

            if (await db.Installations.AnyAsync(i => i.ProjectId == project.Id, cancellationToken))
            {
                throw new ConflictException("El proyecto ya tiene una instalación asociada.");
            }

            client = project.Client;
            defaultName = $"Instalación de proyecto {project.Code}";
            defaultLocation = project.Location ?? project.ClientAddress?.ToSingleLine();
        }

        if (!client.IsActive)
        {
            throw new ConflictException("El cliente está inactivo y no admite instalaciones.");
        }

        await SalesSupport.EnsureAddressBelongsToClientAsync(
            db, client.Id, request.ClientAddressId, cancellationToken);

        await SalesSupport.EnsureActiveWorkerAsync(
            db, request.TechnicianWorkerId, cancellationToken);

        var installation = new Installation
        {
            Code = await db.Installations
                .Select(i => i.Code)
                .NextSequentialCodeAsync("INS-", cancellationToken),
            ClientId = client.Id,
            SaleId = sale?.Id,
            ProjectId = project?.Id,
            ClientAddressId = request.ClientAddressId,
            Location = request.Location?.Trim() ?? defaultLocation,
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? defaultName
                : request.Name.Trim(),
            Description = request.Description?.Trim(),
            Status = InstallationStatus.Scheduled,
            ScheduledDate = request.ScheduledDate,
            TechnicianWorkerId = request.TechnicianWorkerId,
            Notes = request.Notes?.Trim(),
        };

        db.Installations.Add(installation);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "installations.created",
            nameof(Installation),
            installation.Id.ToString(),
            details:
                $"Instalación: {installation.Code} - {installation.Name}. "
                + $"Origen: {(sale is not null ? $"venta {sale.Code}" : $"proyecto {project!.Code}")}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(installation.Id, cancellationToken);
    }

    public async Task<InstallationDto> UpdateAsync(
        Guid id,
        UpdateInstallationRequest request,
        CancellationToken cancellationToken)
    {
        var installation = await GetInstallationOrThrowAsync(id, cancellationToken);

        if (SalesSupport.IsFinal(installation.Status))
        {
            throw new ConflictException(
                "Una instalación completada o anulada no se puede editar.");
        }

        await SalesSupport.EnsureAddressBelongsToClientAsync(
            db, installation.ClientId, request.ClientAddressId, cancellationToken);

        await SalesSupport.EnsureActiveWorkerAsync(
            db, request.TechnicianWorkerId, cancellationToken);

        installation.ClientAddressId = request.ClientAddressId;
        installation.Location = request.Location?.Trim();
        installation.Name = request.Name.Trim();
        installation.Description = request.Description?.Trim();
        installation.ScheduledDate = request.ScheduledDate;
        installation.TechnicianWorkerId = request.TechnicianWorkerId;
        installation.Notes = request.Notes?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "installations.updated",
            nameof(Installation),
            installation.Id.ToString(),
            details: $"Instalación actualizada: {installation.Code} - {installation.Name}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(installation.Id, cancellationToken);
    }

    public async Task<InstallationDto> StartAsync(
        Guid id,
        StartInstallationRequest request,
        CancellationToken cancellationToken)
    {
        var installation = await GetInstallationOrThrowAsync(id, cancellationToken);

        if (installation.Status != InstallationStatus.Scheduled)
        {
            throw new ConflictException(
                $"La instalación está en estado '{installation.Status}' y no se puede iniciar.");
        }

        var today = SalesSupport.Today(clock);
        var startedDate = request.StartedDate ?? today;

        if (startedDate > today)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["StartedDate"] = ["La fecha de inicio no puede ser posterior a hoy."],
            });
        }

        installation.StartedDate = startedDate;
        installation.Status = InstallationStatus.InProgress;

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "installations.started",
            nameof(Installation),
            installation.Id.ToString(),
            details: $"Instalación iniciada: {installation.Code}. Fecha: {startedDate}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(installation.Id, cancellationToken);
    }

    public async Task<InstallationDto> CompleteAsync(
        Guid id,
        CompleteInstallationRequest request,
        CancellationToken cancellationToken)
    {
        var installation = await GetInstallationOrThrowAsync(id, cancellationToken);

        if (installation.Status != InstallationStatus.InProgress)
        {
            throw new ConflictException(
                $"La instalación está en estado '{installation.Status}' y no se puede completar. "
                + "Iníciela primero para registrar la fecha de inicio.");
        }

        var today = SalesSupport.Today(clock);
        var completedDate = request.CompletedDate ?? today;

        if (completedDate > today)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["CompletedDate"] = ["La fecha de finalización no puede ser posterior a hoy."],
            });
        }

        if (installation.StartedDate is not null && completedDate < installation.StartedDate)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["CompletedDate"] =
                    ["La fecha de finalización no puede ser anterior a la fecha de inicio."],
            });
        }

        installation.CompletedDate = completedDate;
        installation.Status = InstallationStatus.Completed;

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            installation.Notes = request.Notes.Trim();
        }

        var saleCompleted = false;

        if (installation.Sale is { Status: SaleStatus.Delivered } sale)
        {
            sale.Status = SaleStatus.Completed;
            sale.CompletedAt = clock.UtcNow;
            saleCompleted = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "installations.completed",
            nameof(Installation),
            installation.Id.ToString(),
            details: $"Instalación completada: {installation.Code}. Fecha: {completedDate}.",
            cancellationToken: cancellationToken);

        if (saleCompleted && installation.Sale is not null)
        {
            await auditService.LogAsync(
                "sales.completed",
                nameof(Sale),
                installation.Sale.Id.ToString(),
                details:
                    $"Venta {installation.Sale.Code} cerrada al completar la instalación "
                    + $"{installation.Code}.",
                cancellationToken: cancellationToken);
        }

        return await GetByIdAsync(installation.Id, cancellationToken);
    }

    public async Task<InstallationDto> CancelAsync(
        Guid id,
        string reason,
        CancellationToken cancellationToken)
    {
        var installation = await GetInstallationOrThrowAsync(id, cancellationToken);

        if (SalesSupport.IsFinal(installation.Status))
        {
            throw new ConflictException(
                $"La instalación está en estado '{installation.Status}' y no se puede anular.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Reason"] = ["El motivo de anulación es obligatorio."],
            });
        }

        installation.Status = InstallationStatus.Cancelled;
        installation.CancellationReason = reason.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "installations.cancelled",
            nameof(Installation),
            installation.Id.ToString(),
            details:
                $"Instalación anulada: {installation.Code}. Motivo: "
                + $"{installation.CancellationReason}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(installation.Id, cancellationToken);
    }

    private IQueryable<Installation> InstallationQueryWithDetails() =>
        db.Installations
            .Include(i => i.Client)
            .Include(i => i.Sale)
            .Include(i => i.Project)
            .Include(i => i.ClientAddress)
            .Include(i => i.Technician);

    private async Task<Installation> GetInstallationOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await InstallationQueryWithDetails()
            .SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
        ?? throw new NotFoundException("Instalación", id);
}
