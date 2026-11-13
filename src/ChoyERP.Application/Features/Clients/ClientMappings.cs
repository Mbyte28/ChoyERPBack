using ChoyERP.Application.Features.Clients.Dtos;
using ChoyERP.Domain.Entities.Clients;

namespace ChoyERP.Application.Features.Clients;

internal static class ClientMappings
{
    public static ClientAddressDto ToDto(this ClientAddress address) =>
        new(
            address.Id,
            address.Label,
            address.Line1,
            address.Line2,
            address.City,
            address.State,
            address.Country,
            address.PostalCode,
            address.IsPrimary);

    public static ClientDto ToDto(this Client client) =>
        new(
            client.Id,
            client.Code,
            client.Name,
            client.LegalName,
            client.TaxId,
            client.ContactName,
            client.Email,
            client.Phone,
            client.Notes,
            client.IsActive,
            client.Addresses
                .OrderByDescending(a => a.IsPrimary)
                .ThenBy(a => a.Label, StringComparer.OrdinalIgnoreCase)
                .Select(a => a.ToDto())
                .ToArray(),
            client.CreatedAt,
            client.UpdatedAt);
}
