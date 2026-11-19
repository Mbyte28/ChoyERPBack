using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Equipment.Dtos;
using ChoyERP.Domain.Entities.Equipment;
using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Equipment;

public sealed class EquipmentService(
    IAppDbContext db,
    IAuditService auditService,
    IDateTimeProvider clock)
{
    public async Task<PagedResult<EquipmentDto>> GetPagedAsync(
        EquipmentQuery query,
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

        var today = Today();
        var equipment = EquipmentQueryWithDetails().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            equipment = equipment.Where(e =>
                e.Code.ToLower().Contains(search)
                || e.Name.ToLower().Contains(search)
                || (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(search))
                || (e.Location != null && e.Location.ToLower().Contains(search))
                || e.Client.Name.ToLower().Contains(search)
                || e.Installation.Code.ToLower().Contains(search)
                || e.InventoryItem.Code.ToLower().Contains(search)
                || e.InventoryItem.Name.ToLower().Contains(search));
        }

        if (query.Status is not null)
        {
            equipment = equipment.Where(e => e.Status == query.Status);
        }

        if (query.ClientId is not null)
        {
            equipment = equipment.Where(e => e.ClientId == query.ClientId);
        }

        if (query.InstallationId is not null)
        {
            equipment = equipment.Where(e => e.InstallationId == query.InstallationId);
        }

        if (query.InventoryItemId is not null)
        {
            equipment = equipment.Where(e => e.InventoryItemId == query.InventoryItemId);
        }

        if (query.UnderWarranty is not null)
        {
            equipment = query.UnderWarranty.Value
                ? equipment.Where(e => e.WarrantyEndDate != null && e.WarrantyEndDate >= today)
                : equipment.Where(e => e.WarrantyEndDate == null || e.WarrantyEndDate < today);
        }

        if (query.FromDate is not null)
        {
            equipment = equipment.Where(e => e.InstalledDate >= query.FromDate);
        }

        if (query.ToDate is not null)
        {
            equipment = equipment.Where(e => e.InstalledDate <= query.ToDate);
        }

        equipment = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? equipment.OrderByDescending(e => e.Code)
                : equipment.OrderBy(e => e.Code),
            "name" => query.SortDesc
                ? equipment.OrderByDescending(e => e.Name).ThenBy(e => e.Code)
                : equipment.OrderBy(e => e.Name).ThenBy(e => e.Code),
            "status" => query.SortDesc
                ? equipment.OrderByDescending(e => e.Status).ThenBy(e => e.Code)
                : equipment.OrderBy(e => e.Status).ThenBy(e => e.Code),
            "warrantyenddate" => query.SortDesc
                ? equipment.OrderByDescending(e => e.WarrantyEndDate)
                : equipment.OrderBy(e => e.WarrantyEndDate),
            "clientname" => query.SortDesc
                ? equipment.OrderByDescending(e => e.Client.Name)
                : equipment.OrderBy(e => e.Client.Name),
            "createdat" => query.SortDesc
                ? equipment.OrderByDescending(e => e.CreatedAt)
                : equipment.OrderBy(e => e.CreatedAt),
            _ => query.SortDesc
                ? equipment.OrderByDescending(e => e.InstalledDate).ThenByDescending(e => e.Code)
                : equipment.OrderBy(e => e.InstalledDate).ThenBy(e => e.Code),
        };

        var page = await equipment.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<EquipmentDto>(
            page.Items.Select(e => e.ToDto(today)).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<EquipmentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var equipment = await GetEquipmentOrThrowAsync(id, cancellationToken);
        return equipment.ToDto(Today());
    }

    public async Task<IReadOnlyList<EquipmentStatusChangeDto>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!await db.InstalledEquipment.AnyAsync(e => e.Id == id, cancellationToken))
        {
            throw new NotFoundException("Equipo instalado", id);
        }

        var history = await db.EquipmentStatusChanges
            .AsNoTracking()
            .Where(change => change.EquipmentId == id)
            .OrderByDescending(change => change.ChangedDate)
            .ThenByDescending(change => change.CreatedAt)
            .ToListAsync(cancellationToken);

        return history.Select(change => change.ToDto()).ToArray();
    }

    public async Task<EquipmentDto> CreateAsync(
        CreateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var installation = await db.Installations
            .Include(i => i.Client)
            .SingleOrDefaultAsync(i => i.Id == request.InstallationId, cancellationToken)
            ?? throw new NotFoundException("Instalación", request.InstallationId);

        if (installation.Status != InstallationStatus.Completed)
        {
            throw new ConflictException(
                "Solo se pueden registrar equipos en una instalación completada.");
        }

        var item = await db.InventoryItems
            .SingleOrDefaultAsync(i => i.Id == request.InventoryItemId, cancellationToken)
            ?? throw new NotFoundException("Artículo de almacén", request.InventoryItemId);

        if (!item.IsActive)
        {
            throw new ConflictException(
                $"El artículo '{item.Code} - {item.Name}' está inactivo y no puede registrarse como equipo.");
        }

        var today = Today();
        var installedDate = request.InstalledDate ?? installation.CompletedDate ?? today;

        ValidateDates(installedDate, request.WarrantyEndDate, today);

        var serialNumber = NormalizeSerial(request.SerialNumber);
        await EnsureSerialAvailableAsync(item.Id, serialNumber, null, cancellationToken);

        var equipment = new InstalledEquipment
        {
            Code = await db.InstalledEquipment
                .Select(e => e.Code)
                .NextSequentialCodeAsync("EQP-", cancellationToken),
            ClientId = installation.ClientId,
            InstallationId = installation.Id,
            InventoryItemId = item.Id,
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? DefaultName(item)
                : request.Name.Trim(),
            SerialNumber = serialNumber,
            Brand = TrimOrNull(request.Brand) ?? item.Brand,
            Model = TrimOrNull(request.Model) ?? item.Model,
            Location = TrimOrNull(request.Location),
            InstalledDate = installedDate,
            WarrantyEndDate = request.WarrantyEndDate,
            Status = EquipmentStatus.Operational,
            Notes = TrimOrNull(request.Notes),
        };

        equipment.StatusHistory.Add(new EquipmentStatusChange
        {
            EquipmentId = equipment.Id,
            FromStatus = null,
            ToStatus = EquipmentStatus.Operational,
            ChangedDate = installedDate,
            Reason = "Registro inicial del equipo.",
        });

        db.InstalledEquipment.Add(equipment);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "equipment.created",
            nameof(InstalledEquipment),
            equipment.Id.ToString(),
            details:
                $"Equipo: {equipment.Code} - {equipment.Name}. "
                + $"Serie: {equipment.SerialNumber ?? "sin serie"}. "
                + $"Instalación: {installation.Code}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(equipment.Id, cancellationToken);
    }

    public async Task<EquipmentDto> UpdateAsync(
        Guid id,
        UpdateEquipmentRequest request,
        CancellationToken cancellationToken)
    {
        var equipment = await GetEquipmentOrThrowAsync(id, cancellationToken);

        if (equipment.IsRetired)
        {
            throw new ConflictException("Un equipo dado de baja no se puede editar.");
        }

        var serialNumber = NormalizeSerial(request.SerialNumber);
        await EnsureSerialAvailableAsync(
            equipment.InventoryItemId, serialNumber, equipment.Id, cancellationToken);

        if (request.WarrantyEndDate is not null
            && request.WarrantyEndDate < equipment.InstalledDate)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["WarrantyEndDate"] =
                    ["El fin de garantía no puede ser anterior a la fecha de instalación."],
            });
        }

        equipment.Name = request.Name.Trim();
        equipment.SerialNumber = serialNumber;
        equipment.Brand = TrimOrNull(request.Brand);
        equipment.Model = TrimOrNull(request.Model);
        equipment.Location = TrimOrNull(request.Location);
        equipment.WarrantyEndDate = request.WarrantyEndDate;
        equipment.Notes = TrimOrNull(request.Notes);

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "equipment.updated",
            nameof(InstalledEquipment),
            equipment.Id.ToString(),
            details:
                $"Equipo actualizado: {equipment.Code} - {equipment.Name}. "
                + $"Serie: {equipment.SerialNumber ?? "sin serie"}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(equipment.Id, cancellationToken);
    }

    public async Task<EquipmentDto> ChangeStatusAsync(
        Guid id,
        ChangeEquipmentStatusRequest request,
        CancellationToken cancellationToken)
    {
        var equipment = await GetEquipmentOrThrowAsync(id, cancellationToken);

        if (equipment.IsRetired)
        {
            throw new ConflictException(
                "El equipo está dado de baja y no admite más cambios de estado.");
        }

        if (equipment.Status == request.Status)
        {
            throw new ConflictException(
                $"El equipo ya está en estado '{equipment.Status}'.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["Reason"] = ["El motivo del cambio de estado es obligatorio."],
            });
        }

        var today = Today();
        var changedDate = request.ChangedDate ?? today;

        if (changedDate > today)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["ChangedDate"] = ["La fecha del cambio de estado no puede ser posterior a hoy."],
            });
        }

        if (changedDate < equipment.InstalledDate)
        {
            throw new ValidationAppException(new Dictionary<string, string[]>
            {
                ["ChangedDate"] =
                    ["La fecha del cambio de estado no puede ser anterior a la instalación."],
            });
        }

        var previousStatus = equipment.Status;
        var reason = request.Reason.Trim();

        equipment.Status = request.Status;

        db.EquipmentStatusChanges.Add(new EquipmentStatusChange
        {
            EquipmentId = equipment.Id,
            FromStatus = previousStatus,
            ToStatus = request.Status,
            ChangedDate = changedDate,
            Reason = reason,
        });

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "equipment.status_changed",
            nameof(InstalledEquipment),
            equipment.Id.ToString(),
            details:
                $"Equipo: {equipment.Code} - {equipment.Name}. "
                + $"Estado: {previousStatus} -> {request.Status}. Motivo: {reason}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(equipment.Id, cancellationToken);
    }

    private static void ValidateDates(
        DateOnly installedDate,
        DateOnly? warrantyEndDate,
        DateOnly today)
    {
        var errors = new Dictionary<string, string[]>();

        if (installedDate > today)
        {
            errors["InstalledDate"] = ["La fecha de instalación no puede ser posterior a hoy."];
        }

        if (warrantyEndDate is not null && warrantyEndDate < installedDate)
        {
            errors["WarrantyEndDate"] =
                ["El fin de garantía no puede ser anterior a la fecha de instalación."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationAppException(errors);
        }
    }

    private async Task EnsureSerialAvailableAsync(
        Guid inventoryItemId,
        string? serialNumber,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (serialNumber is null)
        {
            return;
        }

        var exists = await db.InstalledEquipment.AnyAsync(
            e => e.InventoryItemId == inventoryItemId
                && e.SerialNumber == serialNumber
                && (excludeId == null || e.Id != excludeId),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                "Ya existe un equipo con ese número de serie para el mismo artículo.");
        }
    }

    private IQueryable<InstalledEquipment> EquipmentQueryWithDetails() =>
        db.InstalledEquipment
            .Include(e => e.Client)
            .Include(e => e.Installation)
            .Include(e => e.InventoryItem);

    private async Task<InstalledEquipment> GetEquipmentOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        await EquipmentQueryWithDetails()
            .SingleOrDefaultAsync(e => e.Id == id, cancellationToken)
        ?? throw new NotFoundException("Equipo instalado", id);

    private DateOnly Today() => DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);

    private static string DefaultName(InventoryItem item) =>
        string.IsNullOrWhiteSpace(item.Model)
            ? item.Name
            : $"{item.Name} ({item.Model})";

    private static string? NormalizeSerial(string? serialNumber) =>
        string.IsNullOrWhiteSpace(serialNumber)
            ? null
            : serialNumber.Trim().ToUpperInvariant();

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
