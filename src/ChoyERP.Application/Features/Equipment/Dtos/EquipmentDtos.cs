using ChoyERP.Application.Common.Models;
using ChoyERP.Domain.Entities.Equipment;

namespace ChoyERP.Application.Features.Equipment.Dtos;

public sealed record EquipmentDto(
    Guid Id,
    string Code,
    Guid ClientId,
    string ClientName,
    Guid InstallationId,
    string InstallationCode,
    Guid InventoryItemId,
    string ItemCode,
    string ItemName,
    string Name,
    string? SerialNumber,
    string? Brand,
    string? Model,
    string? Location,
    DateOnly InstalledDate,
    DateOnly? WarrantyEndDate,
    bool IsUnderWarranty,
    EquipmentStatus Status,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record EquipmentStatusChangeDto(
    Guid Id,
    Guid EquipmentId,
    EquipmentStatus? FromStatus,
    EquipmentStatus ToStatus,
    DateOnly ChangedDate,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record CreateEquipmentRequest(
    Guid InstallationId,
    Guid InventoryItemId,
    string? Name,
    string? SerialNumber,
    string? Brand,
    string? Model,
    string? Location,
    DateOnly? InstalledDate,
    DateOnly? WarrantyEndDate,
    string? Notes);

public sealed record UpdateEquipmentRequest(
    string Name,
    string? SerialNumber,
    string? Brand,
    string? Model,
    string? Location,
    DateOnly? WarrantyEndDate,
    string? Notes);

public sealed record ChangeEquipmentStatusRequest(
    EquipmentStatus Status,
    DateOnly? ChangedDate,
    string Reason);

public sealed class EquipmentQuery : PaginationQuery
{
    public string? Search { get; set; }

    public EquipmentStatus? Status { get; set; }

    public Guid? ClientId { get; set; }

    public Guid? InstallationId { get; set; }

    public Guid? InventoryItemId { get; set; }

    /// <summary>Si es verdadero, solo equipos con garantía vigente; si es falso, vencida o sin garantía.</summary>
    public bool? UnderWarranty { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}
