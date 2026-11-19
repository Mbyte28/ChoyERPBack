using ChoyERP.Application.Features.Sales.Dtos;
using ChoyERP.Domain.Entities.Sales;

namespace ChoyERP.Application.Features.Sales;

internal static class SalesMappings
{
    public static SaleItemDto ToDto(this SaleItem item) =>
        new(
            item.Id,
            item.InventoryItemId,
            item.InventoryItem.Code,
            item.InventoryItem.Name,
            item.Description,
            item.Unit,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal);

    public static SaleDto ToDto(this Sale sale) =>
        new(
            sale.Id,
            sale.Code,
            sale.ClientId,
            sale.Client.Name,
            sale.ProjectId,
            sale.Project?.Code,
            sale.Project?.Name,
            sale.Status,
            sale.Currency,
            sale.TaxRate,
            sale.Subtotal,
            sale.TaxAmount,
            sale.Total,
            sale.SaleDate,
            sale.ExpectedDeliveryDate,
            sale.DeliveredDate,
            sale.ConfirmedAt,
            sale.CompletedAt,
            sale.CancelledAt,
            sale.CancellationReason,
            sale.Notes,
            sale.Installation?.Id,
            sale.Installation?.Code,
            sale.Installation?.Status,
            sale.Items
                .OrderBy(item => item.SortOrder)
                .Select(item => item.ToDto())
                .ToArray(),
            sale.CreatedAt,
            sale.UpdatedAt);
}
