using ChoyERP.Application.Common.Models;

namespace ChoyERP.Application.Features.Clients.Dtos;

public sealed record ClientAddressDto(
    Guid Id,
    string Label,
    string Line1,
    string? Line2,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    bool IsPrimary);

public sealed record ClientDto(
    Guid Id,
    string Code,
    string Name,
    string? LegalName,
    string? TaxId,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Notes,
    bool IsActive,
    IReadOnlyList<ClientAddressDto> Addresses,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateClientRequest(
    string? Code,
    string Name,
    string? LegalName,
    string? TaxId,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Notes);

public sealed record UpdateClientRequest(
    string Code,
    string Name,
    string? LegalName,
    string? TaxId,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Notes);

public sealed record CreateClientAddressRequest(
    string Label,
    string Line1,
    string? Line2,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    bool IsPrimary);

public sealed record UpdateClientAddressRequest(
    string Label,
    string Line1,
    string? Line2,
    string? City,
    string? State,
    string? Country,
    string? PostalCode,
    bool IsPrimary);

public sealed class ClientQuery : PaginationQuery
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}
