using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Workers.Dtos;
using ChoyERP.Domain.Entities.Workers;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Workers;

public sealed class WorkerService(IAppDbContext db, IAuditService auditService)
{
    public async Task<PagedResult<WorkerDto>> GetPagedAsync(
        WorkerQuery query,
        CancellationToken cancellationToken)
    {
        var workers = db.Workers
            .AsNoTracking()
            .Include(w => w.UserAccount)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            workers = workers.Where(w =>
                w.FirstName.ToLower().Contains(search)
                || w.LastName.ToLower().Contains(search)
                || w.DocumentNumber.ToLower().Contains(search)
                || (w.Email != null && w.Email.ToLower().Contains(search))
                || (w.Position != null && w.Position.ToLower().Contains(search)));
        }

        if (query.IsActive is not null)
        {
            workers = workers.Where(w => w.IsActive == query.IsActive);
        }

        workers = (query.SortBy?.ToLowerInvariant()) switch
        {
            "documentnumber" => query.SortDesc
                ? workers.OrderByDescending(w => w.DocumentNumber)
                : workers.OrderBy(w => w.DocumentNumber),
            "createdat" => query.SortDesc
                ? workers.OrderByDescending(w => w.CreatedAt)
                : workers.OrderBy(w => w.CreatedAt),
            "hiredate" => query.SortDesc
                ? workers.OrderByDescending(w => w.HireDate)
                : workers.OrderBy(w => w.HireDate),
            _ => query.SortDesc
                ? workers.OrderByDescending(w => w.LastName).ThenByDescending(w => w.FirstName)
                : workers.OrderBy(w => w.LastName).ThenBy(w => w.FirstName),
        };

        var page = await workers.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<WorkerDto>(
            page.Items.Select(w => w.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<WorkerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var worker = await GetWorkerOrThrowAsync(id, cancellationToken);
        return worker.ToDto();
    }

    public async Task<WorkerDto> CreateAsync(
        CreateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        var documentNumber = NormalizeDocument(request.DocumentNumber);

        if (await db.Workers.AnyAsync(w => w.DocumentNumber == documentNumber, cancellationToken))
        {
            throw new ConflictException("Ya existe un trabajador con ese número de documento.");
        }

        var worker = new Worker
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            DocumentType = request.DocumentType?.Trim(),
            DocumentNumber = documentNumber,
            Position = request.Position?.Trim(),
            Department = request.Department?.Trim(),
            Phone = request.Phone?.Trim(),
            Email = request.Email?.Trim(),
            HireDate = request.HireDate,
            Notes = request.Notes?.Trim(),
        };

        db.Workers.Add(worker);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "workers.created",
            nameof(Worker),
            worker.Id.ToString(),
            details: $"Trabajador: {worker.FullName}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(worker.Id, cancellationToken);
    }

    public async Task<WorkerDto> UpdateAsync(
        Guid id,
        UpdateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        var worker = await GetWorkerOrThrowAsync(id, cancellationToken);
        var documentNumber = NormalizeDocument(request.DocumentNumber);

        if (await db.Workers.AnyAsync(
                w => w.DocumentNumber == documentNumber && w.Id != id,
                cancellationToken))
        {
            throw new ConflictException("Ya existe un trabajador con ese número de documento.");
        }

        worker.FirstName = request.FirstName.Trim();
        worker.LastName = request.LastName.Trim();
        worker.DocumentType = request.DocumentType?.Trim();
        worker.DocumentNumber = documentNumber;
        worker.Position = request.Position?.Trim();
        worker.Department = request.Department?.Trim();
        worker.Phone = request.Phone?.Trim();
        worker.Email = request.Email?.Trim();
        worker.HireDate = request.HireDate;
        worker.Notes = request.Notes?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "workers.updated",
            nameof(Worker),
            worker.Id.ToString(),
            details: $"Trabajador: {worker.FullName}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(worker.Id, cancellationToken);
    }

    public async Task<WorkerDto> SetStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var worker = await GetWorkerOrThrowAsync(id, cancellationToken);

        if (worker.IsActive == isActive)
        {
            return worker.ToDto();
        }

        if (!isActive && worker.UserAccount is { IsActive: true })
        {
            throw new ConflictException(
                "El trabajador tiene una cuenta de usuario activa. Desactive la cuenta antes de desactivar al trabajador.");
        }

        worker.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            isActive ? "workers.activated" : "workers.deactivated",
            nameof(Worker),
            worker.Id.ToString(),
            cancellationToken: cancellationToken);

        return await GetByIdAsync(worker.Id, cancellationToken);
    }

    private async Task<Worker> GetWorkerOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Workers
            .Include(w => w.UserAccount)
            .SingleOrDefaultAsync(w => w.Id == id, cancellationToken)
        ?? throw new NotFoundException("Trabajador", id);

    private static string NormalizeDocument(string documentNumber) =>
        documentNumber.Trim().ToUpperInvariant();
}
