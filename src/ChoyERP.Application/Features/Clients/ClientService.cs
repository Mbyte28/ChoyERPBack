using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Common.Extensions;
using ChoyERP.Application.Common.Interfaces;
using ChoyERP.Application.Common.Models;
using ChoyERP.Application.Features.Clients.Dtos;
using ChoyERP.Domain.Entities.Clients;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.Application.Features.Clients;

public sealed class ClientService(IAppDbContext db, IAuditService auditService)
{
    public async Task<PagedResult<ClientDto>> GetPagedAsync(
        ClientQuery query,
        CancellationToken cancellationToken)
    {
        var clients = db.Clients
            .AsNoTracking()
            .Include(c => c.Addresses)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            clients = clients.Where(c =>
                c.Code.ToLower().Contains(search)
                || c.Name.ToLower().Contains(search)
                || (c.LegalName != null && c.LegalName.ToLower().Contains(search))
                || (c.TaxId != null && c.TaxId.ToLower().Contains(search))
                || (c.ContactName != null && c.ContactName.ToLower().Contains(search))
                || (c.Email != null && c.Email.ToLower().Contains(search)));
        }

        if (query.IsActive is not null)
        {
            clients = clients.Where(c => c.IsActive == query.IsActive);
        }

        clients = (query.SortBy?.ToLowerInvariant()) switch
        {
            "code" => query.SortDesc
                ? clients.OrderByDescending(c => c.Code)
                : clients.OrderBy(c => c.Code),
            "createdat" => query.SortDesc
                ? clients.OrderByDescending(c => c.CreatedAt)
                : clients.OrderBy(c => c.CreatedAt),
            "isactive" => query.SortDesc
                ? clients.OrderByDescending(c => c.IsActive)
                : clients.OrderBy(c => c.IsActive),
            _ => query.SortDesc
                ? clients.OrderByDescending(c => c.Name)
                : clients.OrderBy(c => c.Name),
        };

        var page = await clients.ToPagedResultAsync(query, cancellationToken);
        return new PagedResult<ClientDto>(
            page.Items.Select(c => c.ToDto()).ToArray(),
            page.Page,
            page.PageSize,
            page.TotalItems);
    }

    public async Task<ClientDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var client = await GetClientOrThrowAsync(id, cancellationToken);
        return client.ToDto();
    }

    public async Task<ClientDto> CreateAsync(
        CreateClientRequest request,
        CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(request.Code)
            ? await GenerateCodeAsync(cancellationToken)
            : request.Code.Trim().ToUpperInvariant();

        if (await db.Clients.AnyAsync(c => c.Code == code, cancellationToken))
        {
            throw new ConflictException("Ya existe un cliente con ese código.");
        }

        var taxId = NormalizeTaxId(request.TaxId);

        if (taxId is not null && await db.Clients.AnyAsync(c => c.TaxId == taxId, cancellationToken))
        {
            throw new ConflictException("Ya existe un cliente con ese identificador tributario.");
        }

        var client = new Client
        {
            Code = code,
            Name = request.Name.Trim(),
            LegalName = request.LegalName?.Trim(),
            TaxId = taxId,
            ContactName = request.ContactName?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Notes = request.Notes?.Trim(),
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "clients.created",
            nameof(Client),
            client.Id.ToString(),
            details: $"Cliente: {client.Code} - {client.Name}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(client.Id, cancellationToken);
    }

    public async Task<ClientDto> UpdateAsync(
        Guid id,
        UpdateClientRequest request,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrThrowAsync(id, cancellationToken);
        var code = request.Code.Trim().ToUpperInvariant();

        if (await db.Clients.AnyAsync(c => c.Code == code && c.Id != id, cancellationToken))
        {
            throw new ConflictException("Ya existe un cliente con ese código.");
        }

        var taxId = NormalizeTaxId(request.TaxId);

        if (taxId is not null
            && await db.Clients.AnyAsync(c => c.TaxId == taxId && c.Id != id, cancellationToken))
        {
            throw new ConflictException("Ya existe un cliente con ese identificador tributario.");
        }

        client.Code = code;
        client.Name = request.Name.Trim();
        client.LegalName = request.LegalName?.Trim();
        client.TaxId = taxId;
        client.ContactName = request.ContactName?.Trim();
        client.Email = request.Email?.Trim();
        client.Phone = request.Phone?.Trim();
        client.Notes = request.Notes?.Trim();

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "clients.updated",
            nameof(Client),
            client.Id.ToString(),
            details: $"Cliente: {client.Code} - {client.Name}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(client.Id, cancellationToken);
    }

    public async Task<ClientDto> SetStatusAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrThrowAsync(id, cancellationToken);

        if (client.IsActive == isActive)
        {
            return client.ToDto();
        }

        client.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            isActive ? "clients.activated" : "clients.deactivated",
            nameof(Client),
            client.Id.ToString(),
            details: $"Cliente: {client.Code} - {client.Name}.",
            cancellationToken: cancellationToken);

        return await GetByIdAsync(client.Id, cancellationToken);
    }

    public async Task<ClientAddressDto> AddAddressAsync(
        Guid clientId,
        CreateClientAddressRequest request,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrThrowAsync(clientId, cancellationToken);

        var makePrimary = request.IsPrimary || client.Addresses.Count == 0;

        if (makePrimary)
        {
            foreach (var address in client.Addresses)
            {
                address.IsPrimary = false;
            }
        }

        var newAddress = new ClientAddress
        {
            ClientId = client.Id,
            Label = request.Label.Trim(),
            Line1 = request.Line1.Trim(),
            Line2 = request.Line2?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            Country = request.Country?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            IsPrimary = makePrimary,
        };

        db.ClientAddresses.Add(newAddress);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "clients.address_added",
            nameof(ClientAddress),
            newAddress.Id.ToString(),
            details: $"Cliente: {client.Code}. Dirección: {newAddress.Label}.",
            cancellationToken: cancellationToken);

        return newAddress.ToDto();
    }

    public async Task<ClientAddressDto> UpdateAddressAsync(
        Guid clientId,
        Guid addressId,
        UpdateClientAddressRequest request,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrThrowAsync(clientId, cancellationToken);
        var address = client.Addresses.SingleOrDefault(a => a.Id == addressId)
            ?? throw new NotFoundException("Dirección", addressId);

        address.Label = request.Label.Trim();
        address.Line1 = request.Line1.Trim();
        address.Line2 = request.Line2?.Trim();
        address.City = request.City?.Trim();
        address.State = request.State?.Trim();
        address.Country = request.Country?.Trim();
        address.PostalCode = request.PostalCode?.Trim();

        if (request.IsPrimary)
        {
            foreach (var other in client.Addresses.Where(a => a.Id != address.Id))
            {
                other.IsPrimary = false;
            }

            address.IsPrimary = true;
        }
        else if (address.IsPrimary && client.Addresses.Count > 1)
        {
            address.IsPrimary = false;
            var replacement = client.Addresses
                .Where(a => a.Id != address.Id)
                .OrderBy(a => a.CreatedAt)
                .First();
            replacement.IsPrimary = true;
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "clients.address_updated",
            nameof(ClientAddress),
            address.Id.ToString(),
            details: $"Cliente: {client.Code}. Dirección: {address.Label}.",
            cancellationToken: cancellationToken);

        return address.ToDto();
    }

    public async Task DeleteAddressAsync(
        Guid clientId,
        Guid addressId,
        CancellationToken cancellationToken)
    {
        var client = await GetClientOrThrowAsync(clientId, cancellationToken);
        var address = client.Addresses.SingleOrDefault(a => a.Id == addressId)
            ?? throw new NotFoundException("Dirección", addressId);

        var wasPrimary = address.IsPrimary;

        db.ClientAddresses.Remove(address);

        if (wasPrimary)
        {
            var replacement = client.Addresses
                .Where(a => a.Id != addressId)
                .OrderBy(a => a.CreatedAt)
                .FirstOrDefault();

            if (replacement is not null)
            {
                replacement.IsPrimary = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "clients.address_deleted",
            nameof(ClientAddress),
            addressId.ToString(),
            details: $"Cliente: {client.Code}. Dirección eliminada: {address.Label}.",
            cancellationToken: cancellationToken);
    }

    private async Task<Client> GetClientOrThrowAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Clients
            .Include(c => c.Addresses)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
        ?? throw new NotFoundException("Cliente", id);

    private async Task<string> GenerateCodeAsync(CancellationToken cancellationToken)
    {
        var lastCode = await db.Clients
            .OrderByDescending(c => c.Code)
            .Select(c => c.Code)
            .FirstOrDefaultAsync(cancellationToken);

        var nextNumber = 1;

        if (lastCode is not null
            && lastCode.StartsWith("CLI-", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(lastCode[4..], out var parsed))
        {
            nextNumber = parsed + 1;
        }

        return $"CLI-{nextNumber:D5}";
    }

    private static string? NormalizeTaxId(string? taxId) =>
        string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim().ToUpperInvariant();
}
