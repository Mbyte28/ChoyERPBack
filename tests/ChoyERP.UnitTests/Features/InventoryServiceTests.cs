using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Features.Inventory;
using ChoyERP.Application.Features.Inventory.Dtos;
using ChoyERP.Application.Features.Inventory.Validators;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Inventory;
using ChoyERP.Infrastructure.Persistence;

namespace ChoyERP.UnitTests.Features;

public sealed class InventoryServiceTests
{
    private static InventoryItemService NewItemService(AppDbContext db, FakeAuditService audit) =>
        new(db, audit);

    private static InventoryMovementService NewMovementService(
        AppDbContext db,
        FakeAuditService audit,
        FixedDateTimeProvider clock) =>
        new(db, audit, clock);

    private static CreateInventoryItemRequest NewItem(
        string name = "Filtro secador 3/8\"",
        decimal minimumStock = 10m,
        decimal unitCost = 12m,
        string? code = null) =>
        new(
            code,
            name,
            "Filtro para línea de líquido",
            InventoryCategory.Spare,
            InventoryUnit.Unit,
            "Danfoss",
            "DML-033",
            "Cajón C-3",
            minimumStock,
            unitCost,
            null);

    private static CreateInventoryMovementRequest NewEntry(
        Guid itemId,
        decimal quantity,
        decimal? unitCost) =>
        new(
            itemId,
            InventoryMovementType.Entry,
            quantity,
            unitCost,
            null,
            "Compra a proveedor",
            "FAC-0001",
            null,
            null);

    private static CreateInventoryMovementRequest NewExit(
        Guid itemId,
        decimal quantity,
        Guid? projectId = null) =>
        new(
            itemId,
            InventoryMovementType.Exit,
            quantity,
            null,
            null,
            "Consumo en obra",
            "OT-0001",
            projectId,
            null);

    private static CreateInventoryMovementRequest NewAdjustment(
        Guid itemId,
        decimal countedStock,
        string? reason = "Conteo físico mensual") =>
        new(
            itemId,
            InventoryMovementType.Adjustment,
            countedStock,
            null,
            null,
            reason,
            null,
            null,
            null);

    [Fact]
    public async Task CreateItem_GeneratesCode_AndStartsWithoutStock()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var items = NewItemService(db, audit);

        var first = await items.CreateAsync(NewItem(), CancellationToken.None);
        var second = await items.CreateAsync(NewItem("Tubería de cobre", code: "rep-001"), CancellationToken.None);

        Assert.Equal("ART-00001", first.Code);
        Assert.Equal(0m, first.QuantityOnHand);
        Assert.Equal(12m, first.AverageUnitCost);
        Assert.True(first.IsBelowMinimum);
        Assert.True(first.IsActive);

        Assert.Equal("REP-001", second.Code);
        Assert.Equal("ART-00002", (await items.CreateAsync(NewItem("Válvula"), CancellationToken.None)).Code);

        await Assert.ThrowsAsync<ConflictException>(() =>
            items.CreateAsync(NewItem("Duplicado", code: "REP-001"), CancellationToken.None));

        Assert.Contains("inventory.items.created", audit.Actions);
    }

    [Fact]
    public async Task Entry_UpdatesStock_AndWeightedAverageCost()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var items = NewItemService(db, audit);
        var movements = NewMovementService(db, audit, clock);

        var item = await items.CreateAsync(NewItem(unitCost: 10m), CancellationToken.None);

        var firstEntry = await movements.CreateAsync(
            NewEntry(item.Id, 10m, 10m),
            CancellationToken.None);

        Assert.Equal("MOV-00001", firstEntry.Code);
        Assert.Equal(InventoryMovementType.Entry, firstEntry.Type);
        Assert.Equal(10m, firstEntry.QuantityDelta);
        Assert.Equal(0m, firstEntry.StockBefore);
        Assert.Equal(10m, firstEntry.StockAfter);
        Assert.Equal(100m, firstEntry.TotalCost);
        Assert.Equal(new DateOnly(2026, 11, 12), firstEntry.MovementDate);

        var secondEntry = await movements.CreateAsync(
            NewEntry(item.Id, 10m, 20m),
            CancellationToken.None);

        var updated = await items.GetByIdAsync(item.Id, CancellationToken.None);

        Assert.Equal("MOV-00002", secondEntry.Code);
        Assert.Equal(20m, updated.QuantityOnHand);
        Assert.Equal(15m, updated.AverageUnitCost);
        Assert.Equal(300m, updated.TotalValue);
        Assert.False(updated.IsBelowMinimum);
        Assert.Contains("inventory.movements.created", audit.Actions);
    }

    [Fact]
    public async Task Exit_DecreasesStock_AndRejectsInsufficientStock()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var items = NewItemService(db, audit);
        var movements = NewMovementService(db, audit, clock);

        var item = await items.CreateAsync(NewItem(unitCost: 10m), CancellationToken.None);
        await movements.CreateAsync(NewEntry(item.Id, 10m, 10m), CancellationToken.None);

        var exit = await movements.CreateAsync(NewExit(item.Id, 4m), CancellationToken.None);

        Assert.Equal(-4m, exit.QuantityDelta);
        Assert.Equal(10m, exit.StockBefore);
        Assert.Equal(6m, exit.StockAfter);
        Assert.Equal(-40m, exit.TotalCost);

        var conflict = await Assert.ThrowsAsync<ConflictException>(() =>
            movements.CreateAsync(NewExit(item.Id, 7m), CancellationToken.None));

        Assert.Contains("Stock insuficiente", conflict.Message);

        var updated = await items.GetByIdAsync(item.Id, CancellationToken.None);
        Assert.Equal(6m, updated.QuantityOnHand);
    }

    [Fact]
    public async Task Adjustment_SetsCountedStock_AndRejectsNoChange()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var items = NewItemService(db, audit);
        var movements = NewMovementService(db, audit, clock);

        var item = await items.CreateAsync(NewItem(unitCost: 10m), CancellationToken.None);
        await movements.CreateAsync(NewEntry(item.Id, 10m, 10m), CancellationToken.None);

        var adjustment = await movements.CreateAsync(
            NewAdjustment(item.Id, 7m),
            CancellationToken.None);

        Assert.Equal(-3m, adjustment.QuantityDelta);
        Assert.Equal(7m, adjustment.StockAfter);
        Assert.Equal(-30m, adjustment.TotalCost);
        Assert.Equal("Conteo físico mensual", adjustment.Reason);

        await Assert.ThrowsAsync<ConflictException>(() =>
            movements.CreateAsync(NewAdjustment(item.Id, 7m), CancellationToken.None));

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            movements.CreateAsync(NewAdjustment(item.Id, -1m), CancellationToken.None));

        var updated = await items.GetByIdAsync(item.Id, CancellationToken.None);
        Assert.Equal(7m, updated.QuantityOnHand);
    }

    [Fact]
    public async Task DeactivateItem_WithStock_IsConflict_AndInactiveItemRejectsMovements()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var items = NewItemService(db, audit);
        var movements = NewMovementService(db, audit, clock);

        var item = await items.CreateAsync(NewItem(), CancellationToken.None);
        await movements.CreateAsync(NewEntry(item.Id, 5m, 12m), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            items.SetStatusAsync(item.Id, false, CancellationToken.None));

        await movements.CreateAsync(NewExit(item.Id, 5m), CancellationToken.None);

        var inactive = await items.SetStatusAsync(item.Id, false, CancellationToken.None);
        Assert.False(inactive.IsActive);

        await Assert.ThrowsAsync<ConflictException>(() =>
            movements.CreateAsync(NewEntry(item.Id, 1m, 12m), CancellationToken.None));

        var reactivated = await items.SetStatusAsync(item.Id, true, CancellationToken.None);
        Assert.True(reactivated.IsActive);
        Assert.Contains("inventory.items.deactivated", audit.Actions);
        Assert.Contains("inventory.items.activated", audit.Actions);
    }

    [Fact]
    public async Task Movement_WithCancelledProject_IsConflict_AndFutureDateIsRejected()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var items = NewItemService(db, audit);
        var movements = NewMovementService(db, audit, clock);

        var item = await items.CreateAsync(NewItem(), CancellationToken.None);
        await movements.CreateAsync(NewEntry(item.Id, 10m, 12m), CancellationToken.None);

        var project = new Project
        {
            Code = "PRY-00001",
            ContractId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            Name = "Cámara frigorífica",
            Status = ProjectStatus.Cancelled,
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            movements.CreateAsync(NewExit(item.Id, 1m, project.Id), CancellationToken.None));

        var futureMovement = NewEntry(item.Id, 1m, 12m) with
        {
            MovementDate = new DateOnly(2026, 12, 1),
        };

        var futureError = await Assert.ThrowsAsync<ValidationAppException>(() =>
            movements.CreateAsync(futureMovement, CancellationToken.None));

        Assert.Contains("MovementDate", futureError.Errors.Keys);

        var activeProject = new Project
        {
            Code = "PRY-00002",
            ContractId = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            Name = "Túnel de congelamiento",
            Status = ProjectStatus.InProgress,
        };

        db.Projects.Add(activeProject);
        await db.SaveChangesAsync();

        var exit = await movements.CreateAsync(
            NewExit(item.Id, 2m, activeProject.Id),
            CancellationToken.None);

        Assert.Equal(activeProject.Id, exit.ProjectId);
        Assert.Equal("PRY-00002", exit.ProjectCode);
        Assert.Equal("Túnel de congelamiento", exit.ProjectName);
    }

    [Fact]
    public async Task ItemQuery_FiltersBelowMinimum_AndMovementHistoryByItem()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var items = NewItemService(db, audit);
        var movements = NewMovementService(db, audit, clock);

        var below = await items.CreateAsync(NewItem("Compresor", minimumStock: 2m, unitCost: 850m), CancellationToken.None);
        var supplied = await items.CreateAsync(NewItem("Gas refrigerante", minimumStock: 20m, unitCost: 28m), CancellationToken.None);

        await movements.CreateAsync(NewEntry(supplied.Id, 35m, 28m), CancellationToken.None);
        await movements.CreateAsync(NewEntry(supplied.Id, 5m, 30m), CancellationToken.None);

        var lowStock = await items.GetPagedAsync(
            new InventoryItemQuery { BelowMinimum = true },
            CancellationToken.None);

        Assert.Single(lowStock.Items);
        Assert.Equal(below.Id, lowStock.Items[0].Id);

        var history = await movements.GetPagedAsync(
            new InventoryMovementQuery { ItemId = supplied.Id },
            CancellationToken.None);

        Assert.Equal(2, history.TotalItems);
        Assert.All(history.Items, movement => Assert.Equal(supplied.Id, movement.ItemId));

        var entries = await movements.GetPagedAsync(
            new InventoryMovementQuery { Type = InventoryMovementType.Entry },
            CancellationToken.None);

        Assert.Equal(2, entries.TotalItems);
    }

    [Fact]
    public async Task MovementQuery_WithInvertedDateRange_ThrowsValidation()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var movements = NewMovementService(db, audit, clock);

        var error = await Assert.ThrowsAsync<ValidationAppException>(() =>
            movements.GetPagedAsync(
                new InventoryMovementQuery
                {
                    FromDate = new DateOnly(2026, 11, 30),
                    ToDate = new DateOnly(2026, 11, 1),
                },
                CancellationToken.None));

        Assert.Contains("FromDate", error.Errors.Keys);
    }

    [Fact]
    public async Task UpdateItem_ChangingUnitAfterMovements_IsConflict()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var items = NewItemService(db, audit);
        var movements = NewMovementService(db, audit, clock);

        var item = await items.CreateAsync(NewItem(), CancellationToken.None);
        await movements.CreateAsync(NewEntry(item.Id, 2m, 12m), CancellationToken.None);

        var update = new UpdateInventoryItemRequest(
            "Filtro secador 3/8\"",
            "Filtro para línea de líquido",
            InventoryCategory.Spare,
            InventoryUnit.Kilogram,
            "Danfoss",
            "DML-033",
            "Cajón C-3",
            10m,
            null);

        await Assert.ThrowsAsync<ConflictException>(() =>
            items.UpdateAsync(item.Id, update, CancellationToken.None));

        var sameUnit = await items.UpdateAsync(
            item.Id,
            update with { Unit = InventoryUnit.Unit, MinimumStock = 5m },
            CancellationToken.None);

        Assert.Equal(InventoryUnit.Unit, sameUnit.Unit);
        Assert.Equal(5m, sameUnit.MinimumStock);
    }

    [Fact]
    public void MovementValidator_ProducesMemberPropertyNames()
    {
        var validator = new CreateInventoryMovementRequestValidator();

        var valid = validator.Validate(new CreateInventoryMovementRequest(
            Guid.NewGuid(),
            InventoryMovementType.Adjustment,
            5m,
            null,
            null,
            "Conteo físico",
            null,
            null,
            null));

        Assert.True(valid.IsValid);

        var invalid = validator.Validate(new CreateInventoryMovementRequest(
            Guid.NewGuid(),
            InventoryMovementType.Exit,
            0m,
            -1m,
            null,
            null,
            null,
            null,
            null));

        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, error => error.PropertyName == "Quantity");
        Assert.Contains(invalid.Errors, error => error.PropertyName == "UnitCost");
    }

    [Fact]
    public void AdjustmentValidator_RequiresReason()
    {
        var validator = new CreateInventoryMovementRequestValidator();

        var invalid = validator.Validate(new CreateInventoryMovementRequest(
            Guid.NewGuid(),
            InventoryMovementType.Adjustment,
            3m,
            null,
            null,
            "  ",
            null,
            null,
            null));

        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, error => error.PropertyName == "Reason");
    }

    [Fact]
    public void ItemValidator_RejectsNegativeMinimumStockAndCost()
    {
        var validator = new CreateInventoryItemRequestValidator();

        var invalid = validator.Validate(NewItem(minimumStock: -1m, unitCost: -5m));

        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, error => error.PropertyName == "MinimumStock");
        Assert.Contains(invalid.Errors, error => error.PropertyName == "UnitCost");
    }
}
