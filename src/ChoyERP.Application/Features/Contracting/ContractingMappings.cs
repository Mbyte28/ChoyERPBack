using ChoyERP.Application.Features.Contracting.Dtos;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;

namespace ChoyERP.Application.Features.Contracting;

internal static class ContractingMappings
{
    public static QuoteItemDto ToDto(this QuoteItem item) =>
        new(
            item.Id,
            item.Description,
            item.Unit,
            item.Quantity,
            item.UnitPrice,
            item.LineTotal);

    public static QuoteSummaryDto ToSummaryDto(this Quote quote) =>
        new(
            quote.Id,
            quote.Code,
            quote.Version,
            quote.Status,
            quote.Total,
            quote.Currency);

    public static ProjectRequestDto ToDto(this ProjectRequest request) =>
        new(
            request.Id,
            request.Code,
            request.ClientId,
            request.Client.Name,
            request.ClientAddressId,
            request.ClientAddress is null ? null : FormatAddress(request.ClientAddress),
            request.Title,
            request.Description,
            request.ProjectType,
            request.Priority,
            request.Status,
            request.RequestedAt,
            request.ResponsibleWorkerId,
            request.ResponsibleWorker?.FullName,
            request.Notes,
            request.Quotes
                .OrderByDescending(quote => quote.Version)
                .Select(quote => quote.ToSummaryDto())
                .ToArray(),
            request.CreatedAt,
            request.UpdatedAt);

    public static QuoteDto ToDto(this Quote quote) =>
        new(
            quote.Id,
            quote.Code,
            quote.ProjectRequestId,
            quote.ProjectRequest.Code,
            quote.ProjectRequest.Title,
            quote.ProjectRequest.ClientId,
            quote.ProjectRequest.Client.Name,
            quote.Version,
            quote.Status,
            quote.Currency,
            quote.TaxRate,
            quote.Subtotal,
            quote.TaxAmount,
            quote.Total,
            quote.ValidUntil,
            quote.Notes,
            quote.DecisionNotes,
            quote.SentAt,
            quote.AcceptedAt,
            quote.RejectedAt,
            quote.Contract?.Id,
            quote.Contract?.Code,
            quote.Items
                .OrderBy(item => item.SortOrder)
                .Select(item => item.ToDto())
                .ToArray(),
            quote.CreatedAt,
            quote.UpdatedAt);

    public static ContractDto ToDto(this Contract contract) =>
        new(
            contract.Id,
            contract.Code,
            contract.QuoteId,
            contract.Quote.Code,
            contract.Quote.ProjectRequestId,
            contract.Quote.ProjectRequest.Code,
            contract.ClientId,
            contract.Client.Name,
            contract.Status,
            contract.Amount,
            contract.Currency,
            contract.SignedAt,
            contract.StartDate,
            contract.EndDate,
            contract.Notes,
            contract.Project?.Id,
            contract.Project?.Code,
            contract.Project?.Status,
            contract.CreatedAt,
            contract.UpdatedAt);

    public static ProjectDto ToDto(this Project project) =>
        new(
            project.Id,
            project.Code,
            project.ContractId,
            project.Contract.Code,
            project.Contract.QuoteId,
            project.Contract.Quote.Code,
            project.ClientId,
            project.Client.Name,
            project.ClientAddressId,
            project.Location,
            project.Name,
            project.Description,
            project.Status,
            project.ProgressPercent,
            project.StartDate,
            project.ExpectedEndDate,
            project.ActualEndDate,
            project.ResponsibleWorkerId,
            project.ResponsibleWorker?.FullName,
            project.Notes,
            project.CreatedAt,
            project.UpdatedAt);

    public static string FormatAddress(ClientAddress address)
    {
        var parts = new[]
        {
            address.Line1,
            address.Line2,
            address.City,
            address.State,
            address.Country,
        };

        return string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
