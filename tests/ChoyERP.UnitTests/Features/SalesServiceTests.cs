using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Features.Installations;
using ChoyERP.Application.Features.Installations.Dtos;
using ChoyERP.Application.Features.Installations.Validators;
using ChoyERP.Application.Features.Sales;
using ChoyERP.Application.Features.Sales.Dtos;
using ChoyERP.Application.Features.Sales.Validators;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Inventory;
using ChoyERP.Domain.Entities.Sales;
using ChoyERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.UnitTests.Features;

public sealed class SalesServiceTests
{
    private static SaleService NewSaleService(
        AppDbContext db,
        FakeAuditService audit,
        FixedDateTimeProvider clock) =>
        new(db, audit, clock);

    private static async Task<Client> AddClientAsync(
        AppDbContext db,
        string code = "CLI-00001",
        bool isActive = true)
    {
        var client = new Client
        {
            Code = code,
            Name = $"Cliente {code}",
            IsActive = isActive,
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client;
    }

    private static async Task<InventoryItem> AddItemAsync(
        AppDbContext db,
        decimal stock = 10m,
        decimal cost = 80m,
        bool isActive = true,
        string code = "ART-00001",
        string name = "Compresor hermético")
    {
        var item = new InventoryItem
        {
            Code = code,
            Name = name,
            Model = "NEK6210Z",
            Category = InventoryCategory.Equipment,
            Unit = InventoryUnit.Unit,
            QuantityOnHand = stock,
            AverageUnitCost = cost,
            MinimumStock = 0m,
            IsActive = isActive,
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static CreateSaleRequest NewSale(
        Guid clientId,
        params (Guid ItemId, decimal Quantity, decimal UnitPrice)[] lines) =>
        new(
            clientId,
            null,
            "PEN",
            0.18m,
            null,
            null,
            null,
            lines
                .Select(line => new SaleItemRequest(line.ItemId, line.Quantity, line.UnitPrice))
                .ToArray());

    [Fact]
    public async Task CreateSale_GeneratesCodeAndTotals_WithoutChangingStock()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db, stock: 10m, cost: 80m);

        var sale = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 2m, 150m)),
            CancellationToken.None);

        Assert.Equal("VEN-00001", sale.Code);
        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Equal(300m, sale.Subtotal);
        Assert.Equal(54m, sale.TaxAmount);
        Assert.Equal(354m, sale.Total);
        Assert.Equal(new DateOnly(2026, 11, 12), sale.SaleDate);
        Assert.Single(sale.Items);
        Assert.Equal("Compresor hermético (NEK6210Z)", sale.Items[0].Description);
        Assert.Equal("Unit", sale.Items[0].Unit);
        Assert.Equal(300m, sale.Items[0].LineTotal);

        var storedItem = await db.InventoryItems.SingleAsync(i => i.Id == item.Id);
        Assert.Equal(10m, storedItem.QuantityOnHand);

        var second = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 1m, 150m)),
            CancellationToken.None);

        Assert.Equal("VEN-00002", second.Code);
        Assert.Contains("sales.created", audit.Actions);
    }

    [Fact]
    public async Task ConfirmSale_DeductsStock_AndCreatesExitMovement()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db, stock: 10m, cost: 80m);

        var sale = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 3m, 150m)),
            CancellationToken.None);

        var confirmed = await sales.ConfirmAsync(sale.Id, CancellationToken.None);

        Assert.Equal(SaleStatus.Confirmed, confirmed.Status);
        Assert.NotNull(confirmed.ConfirmedAt);

        var storedItem = await db.InventoryItems.SingleAsync(i => i.Id == item.Id);
        Assert.Equal(7m, storedItem.QuantityOnHand);

        var movement = await db.InventoryMovements.SingleAsync();
        Assert.Equal("MOV-00001", movement.Code);
        Assert.Equal(InventoryMovementType.Exit, movement.Type);
        Assert.Equal(sale.Id, movement.SaleId);
        Assert.Equal(sale.Code, movement.Reference);
        Assert.Equal(3m, movement.Quantity);
        Assert.Equal(-3m, movement.QuantityDelta);
        Assert.Equal(10m, movement.StockBefore);
        Assert.Equal(7m, movement.StockAfter);
        Assert.Equal(80m, movement.UnitCost);
        Assert.Equal(-240m, movement.TotalCost);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sales.ConfirmAsync(sale.Id, CancellationToken.None));

        var update = new UpdateSaleRequest(
            client.Id,
            null,
            "PEN",
            0.18m,
            null,
            null,
            null,
            [new SaleItemRequest(item.Id, 1m, 150m)]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sales.UpdateAsync(sale.Id, update, CancellationToken.None));

        Assert.Contains("sales.confirmed", audit.Actions);
        Assert.Contains("inventory.movements.created", audit.Actions);
    }

    [Fact]
    public async Task ConfirmSale_WithInsufficientStock_LeavesSaleInDraft()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db, stock: 1m);

        var sale = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 2m, 150m)),
            CancellationToken.None);

        var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
            sales.ConfirmAsync(sale.Id, CancellationToken.None));

        Assert.Contains("Stock insuficiente", conflict.Message);

        var storedSale = await db.Sales.SingleAsync(s => s.Id == sale.Id);
        var storedItem = await db.InventoryItems.SingleAsync(i => i.Id == item.Id);

        Assert.Equal(SaleStatus.Draft, storedSale.Status);
        Assert.Equal(1m, storedItem.QuantityOnHand);
        Assert.Empty(db.InventoryMovements);
    }

    [Fact]
    public async Task ConfirmSale_RejectsInactiveItems_AndProjectsFromAnotherClient()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);

        var client = await AddClientAsync(db);
        var otherClient = await AddClientAsync(db, "CLI-00002");
        var item = await AddItemAsync(db, stock: 10m);

        var sale = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 1m, 150m)),
            CancellationToken.None);

        item.IsActive = false;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<ConflictException>(() =>
            sales.ConfirmAsync(sale.Id, CancellationToken.None));

        var reloadedItem = await db.InventoryItems.SingleAsync(i => i.Id == item.Id);
        reloadedItem.IsActive = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var foreignProject = new Project
        {
            Code = "PRY-00001",
            ContractId = Guid.NewGuid(),
            ClientId = otherClient.Id,
            Name = "Proyecto de otro cliente",
            Status = ProjectStatus.InProgress,
        };

        db.Projects.Add(foreignProject);
        await db.SaveChangesAsync();

        var foreignRequest = NewSale(client.Id, (item.Id, 1m, 150m)) with
        {
            ProjectId = foreignProject.Id,
        };

        await Assert.ThrowsAsync<ConflictException>(() =>
            sales.CreateAsync(foreignRequest, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateDraftSale_ReplacesItemsAndRecalculatesTotals()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);

        var client = await AddClientAsync(db);
        var firstItem = await AddItemAsync(db, stock: 10m, cost: 80m);
        var secondItem = await AddItemAsync(
            db,
            stock: 5m,
            cost: 20m,
            code: "ART-00002",
            name: "Filtro secador");

        var sale = await sales.CreateAsync(
            NewSale(client.Id, (firstItem.Id, 2m, 100m)),
            CancellationToken.None);

        var updated = await sales.UpdateAsync(
            sale.Id,
            new UpdateSaleRequest(
                client.Id,
                null,
                "USD",
                0.10m,
                null,
                null,
                "Se cambió el equipo",
                [new SaleItemRequest(secondItem.Id, 3m, 50m)]),
            CancellationToken.None);

        Assert.Equal("USD", updated.Currency);
        Assert.Equal(150m, updated.Subtotal);
        Assert.Equal(15m, updated.TaxAmount);
        Assert.Equal(165m, updated.Total);
        Assert.Single(updated.Items);
        Assert.Equal(secondItem.Id, updated.Items[0].InventoryItemId);
        Assert.Equal(150m, updated.Items[0].LineTotal);
        Assert.Equal("Se cambió el equipo", updated.Notes);

        Assert.Single(db.SaleItems);
        Assert.Equal(10m, (await db.InventoryItems.SingleAsync(i => i.Id == firstItem.Id)).QuantityOnHand);
        Assert.Equal(5m, (await db.InventoryItems.SingleAsync(i => i.Id == secondItem.Id)).QuantityOnHand);

        Assert.Contains("sales.updated", audit.Actions);
    }

    [Fact]
    public async Task SaleLifecycle_RequiresDelivery_AndAllowsCompletion()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db, stock: 10m);

        var sale = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 1m, 150m)),
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sales.CompleteAsync(sale.Id, CancellationToken.None));

        await sales.ConfirmAsync(sale.Id, CancellationToken.None);

        var delivered = await sales.DeliverAsync(
            sale.Id,
            new DeliverSaleRequest(null, "Entrega en almacén del cliente"),
            CancellationToken.None);

        Assert.Equal(SaleStatus.Delivered, delivered.Status);
        Assert.Equal(new DateOnly(2026, 11, 12), delivered.DeliveredDate);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sales.DeliverAsync(sale.Id, new DeliverSaleRequest(null, null), CancellationToken.None));

        var completed = await sales.CompleteAsync(sale.Id, CancellationToken.None);

        Assert.Equal(SaleStatus.Completed, completed.Status);
        Assert.NotNull(completed.CompletedAt);
        Assert.Contains("sales.delivered", audit.Actions);
        Assert.Contains("sales.completed", audit.Actions);
    }

    [Fact]
    public async Task CompleteSale_IsBlockedWhileInstallationIsPending()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);
        var installations = new InstallationService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db, stock: 10m);

        var sale = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 1m, 150m)),
            CancellationToken.None);

        await sales.ConfirmAsync(sale.Id, CancellationToken.None);
        await sales.DeliverAsync(sale.Id, new DeliverSaleRequest(null, null), CancellationToken.None);

        var installation = await installations.CreateAsync(
            new CreateInstallationRequest(
                sale.Id, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sales.CompleteAsync(sale.Id, CancellationToken.None));

        await installations.StartAsync(
            installation.Id,
            new StartInstallationRequest(null),
            CancellationToken.None);

        await installations.CompleteAsync(
            installation.Id,
            new CompleteInstallationRequest(null, "Equipo instalado y probado"),
            CancellationToken.None);

        var storedSale = await db.Sales.SingleAsync(s => s.Id == sale.Id);

        Assert.Equal(SaleStatus.Completed, storedSale.Status);
        Assert.Contains("installations.completed", audit.Actions);
        Assert.Contains("sales.completed", audit.Actions);
    }

    [Fact]
    public async Task CancelSale_OnlyAllowedInDraft_AndRequiresReason()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db, stock: 10m);

        var draft = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 1m, 150m)),
            CancellationToken.None);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            sales.CancelAsync(draft.Id, "  ", CancellationToken.None));

        var cancelled = await sales.CancelAsync(
            draft.Id,
            "El cliente desistió de la compra",
            CancellationToken.None);

        Assert.Equal(SaleStatus.Cancelled, cancelled.Status);
        Assert.Equal("El cliente desistió de la compra", cancelled.CancellationReason);
        Assert.NotNull(cancelled.CancelledAt);

        var confirmed = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 1m, 150m)),
            CancellationToken.None);

        await sales.ConfirmAsync(confirmed.Id, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            sales.CancelAsync(confirmed.Id, "Cambio de decisión", CancellationToken.None));
    }

    [Fact]
    public async Task SaleDates_AreValidated()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);

        var future = NewSale(client.Id, (item.Id, 1m, 150m)) with
        {
            SaleDate = new DateOnly(2026, 12, 1),
        };

        var futureError = await Assert.ThrowsAsync<ValidationAppException>(() =>
            sales.CreateAsync(future, CancellationToken.None));

        Assert.Contains("SaleDate", futureError.Errors.Keys);

        var inverted = NewSale(client.Id, (item.Id, 1m, 150m)) with
        {
            SaleDate = new DateOnly(2026, 11, 10),
            ExpectedDeliveryDate = new DateOnly(2026, 11, 9),
        };

        var invertedError = await Assert.ThrowsAsync<ValidationAppException>(() =>
            sales.CreateAsync(inverted, CancellationToken.None));

        Assert.Contains("ExpectedDeliveryDate", invertedError.Errors.Keys);
    }

    [Fact]
    public async Task SaleQuery_FiltersByStatusClientAndDate()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var sales = NewSaleService(db, audit, clock);

        var client = await AddClientAsync(db);
        var otherClient = await AddClientAsync(db, "CLI-00002");
        var item = await AddItemAsync(db, stock: 10m);

        var confirmed = await sales.CreateAsync(
            NewSale(client.Id, (item.Id, 1m, 150m)),
            CancellationToken.None);

        await sales.ConfirmAsync(confirmed.Id, CancellationToken.None);

        await sales.CreateAsync(
            NewSale(otherClient.Id, (item.Id, 1m, 150m)),
            CancellationToken.None);

        var drafts = await sales.GetPagedAsync(
            new SaleQuery { Status = SaleStatus.Draft },
            CancellationToken.None);

        Assert.Single(drafts.Items);

        var clientSales = await sales.GetPagedAsync(
            new SaleQuery { ClientId = client.Id },
            CancellationToken.None);

        Assert.Single(clientSales.Items);
        Assert.Equal(confirmed.Id, clientSales.Items[0].Id);

        var outOfRange = await sales.GetPagedAsync(
            new SaleQuery { FromDate = new DateOnly(2026, 11, 13) },
            CancellationToken.None);

        Assert.Empty(outOfRange.Items);
    }

    [Fact]
    public void Validators_RejectInvalidSales()
    {
        var saleValidator = new CreateSaleRequestValidator();

        var invalid = saleValidator.Validate(new CreateSaleRequest(
            Guid.Empty,
            null,
            "PE",
            1.5m,
            null,
            null,
            null,
            []));

        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, error => error.PropertyName == "ClientId");
        Assert.Contains(invalid.Errors, error => error.PropertyName == "Currency");
        Assert.Contains(invalid.Errors, error => error.PropertyName == "TaxRate");
        Assert.Contains(invalid.Errors, error => error.PropertyName == "Items");

        var itemValidator = new SaleItemRequestValidator();

        var invalidItem = itemValidator.Validate(
            new SaleItemRequest(Guid.NewGuid(), 0m, -1m));

        Assert.False(invalidItem.IsValid);
        Assert.Contains(invalidItem.Errors, error => error.PropertyName == "Quantity");
        Assert.Contains(invalidItem.Errors, error => error.PropertyName == "UnitPrice");

        var cancelValidator = new CancelSaleRequestValidator();

        Assert.False(cancelValidator.Validate(new CancelSaleRequest(" ")).IsValid);
    }

    [Fact]
    public void InstallationValidator_RequiresExactlyOneOrigin()
    {
        var validator = new CreateInstallationRequestValidator();

        var missing = validator.Validate(new CreateInstallationRequest(
            null, null, null, null, null, null, null, null, null));

        Assert.False(missing.IsValid);
        Assert.Contains(missing.Errors, error => error.PropertyName == "SaleId");

        var both = validator.Validate(new CreateInstallationRequest(
            Guid.NewGuid(), Guid.NewGuid(), null, null, null, null, null, null, null));

        Assert.False(both.IsValid);

        var valid = validator.Validate(new CreateInstallationRequest(
            Guid.NewGuid(), null, null, "Planta norte", null, null, null, null, null));

        Assert.True(valid.IsValid);
    }
}
