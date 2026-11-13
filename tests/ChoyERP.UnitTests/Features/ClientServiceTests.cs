using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Features.Clients;
using ChoyERP.Application.Features.Clients.Dtos;
using ChoyERP.Infrastructure.Persistence;

namespace ChoyERP.UnitTests.Features;

public sealed class ClientServiceTests
{
    [Fact]
    public async Task Create_GeneratesCodeAndNormalizesTaxId()
    {
        var db = TestDb.Create();
        var service = new ClientService(db, new FakeAuditService());

        var client = await service.CreateAsync(
            new CreateClientRequest(
                null,
                "Cliente Demo",
                null,
                " 20123456789 ",
                null,
                null,
                null,
                null),
            CancellationToken.None);

        Assert.Equal("CLI-00001", client.Code);
        Assert.Equal("20123456789", client.TaxId);
        Assert.True(client.IsActive);
    }

    [Fact]
    public async Task Create_WithDuplicateTaxId_ThrowsConflict()
    {
        var db = TestDb.Create();
        var service = new ClientService(db, new FakeAuditService());

        var request = new CreateClientRequest(
            null,
            "Cliente Uno",
            null,
            "20123456789",
            null,
            null,
            null,
            null);

        await service.CreateAsync(request, CancellationToken.None);

        var duplicate = request with { Name = "Cliente Dos" };

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CreateAsync(duplicate, CancellationToken.None));
    }

    [Fact]
    public async Task Addresses_FirstIsPrimaryAndNewPrimaryUnsetsPrevious()
    {
        var db = TestDb.Create();
        var service = new ClientService(db, new FakeAuditService());

        var client = await service.CreateAsync(
            new CreateClientRequest(null, "Cliente Direcciones", null, null, null, null, null, null),
            CancellationToken.None);

        var first = await service.AddAddressAsync(
            client.Id,
            new CreateClientAddressRequest("Planta", "Av. Uno 123", null, "Lima", null, "Perú", null, false),
            CancellationToken.None);

        Assert.True(first.IsPrimary);

        var second = await service.AddAddressAsync(
            client.Id,
            new CreateClientAddressRequest("Almacén", "Av. Dos 456", null, "Callao", null, "Perú", null, true),
            CancellationToken.None);

        var updated = await service.GetByIdAsync(client.Id, CancellationToken.None);

        Assert.True(second.IsPrimary);
        Assert.Single(updated.Addresses, address => address.IsPrimary);
        Assert.False(updated.Addresses.Single(address => address.Id == first.Id).IsPrimary);
    }

    [Fact]
    public async Task DeletePrimaryAddress_PromotesAnotherAddress()
    {
        var db = TestDb.Create();
        var service = new ClientService(db, new FakeAuditService());

        var client = await service.CreateAsync(
            new CreateClientRequest(null, "Cliente Direcciones", null, null, null, null, null, null),
            CancellationToken.None);

        var primary = await service.AddAddressAsync(
            client.Id,
            new CreateClientAddressRequest("Planta", "Av. Uno 123", null, null, null, null, null, true),
            CancellationToken.None);

        await service.AddAddressAsync(
            client.Id,
            new CreateClientAddressRequest("Almacén", "Av. Dos 456", null, null, null, null, null, false),
            CancellationToken.None);

        await service.DeleteAddressAsync(client.Id, primary.Id, CancellationToken.None);

        var updated = await service.GetByIdAsync(client.Id, CancellationToken.None);

        Assert.Single(updated.Addresses);
        Assert.True(updated.Addresses[0].IsPrimary);
    }
}
