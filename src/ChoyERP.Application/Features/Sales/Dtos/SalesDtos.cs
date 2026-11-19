using ChoyERP.Application.Common.Models;
using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Sales;

namespace ChoyERP.Application.Features.Sales.Dtos;

public sealed record SaleItemDto(
    Guid Id,
    Guid InventoryItemId,
    string ItemCode,
    string ItemName,
    string Description,
    string? Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record SaleDto(
    Guid Id,
    string Code,
    Guid ClientId,
    string ClientName,
    Guid? ProjectId,
    string? ProjectCode,
    string? ProjectName,
    SaleStatus Status,
    string Currency,
    decimal TaxRate,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    DateOnly SaleDate,
    DateOnly? ExpectedDeliveryDate,
    DateOnly? DeliveredDate,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason,
    string? Notes,
    Guid? InstallationId,
    string? InstallationCode,
    InstallationStatus? InstallationStatus,
    IReadOnlyList<SaleItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record SaleItemRequest(
    Guid InventoryItemId,
    decimal Quantity,
    decimal UnitPrice);

public sealed record CreateSaleRequest(
    Guid ClientId,
    Guid? ProjectId,
    string Currency,
    decimal TaxRate,
    DateOnly? SaleDate,
    DateOnly? ExpectedDeliveryDate,
    string? Notes,
    IReadOnlyList<SaleItemRequest> Items);

public sealed record UpdateSaleRequest(
    Guid ClientId,
    Guid? ProjectId,
    string Currency,
    decimal TaxRate,
    DateOnly? SaleDate,
    DateOnly? ExpectedDeliveryDate,
    string? Notes,
    IReadOnlyList<SaleItemRequest> Items);

public sealed record DeliverSaleRequest(DateOnly? DeliveredDate, string? Notes);

public sealed record CancelSaleRequest(string Reason);

public sealed class SaleQuery : PaginationQuery
{
    public string? Search { get; set; }

    public SaleStatus? Status { get; set; }

    public Guid? ClientId { get; set; }

    public Guid? ProjectId { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}
