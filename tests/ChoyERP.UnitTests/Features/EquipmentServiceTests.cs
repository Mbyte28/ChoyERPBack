using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Features.Equipment;
using ChoyERP.Application.Features.Equipment.Dtos;
using ChoyERP.Application.Features.Equipment.Validators;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Equipment;
using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Inventory;
using ChoyERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.UnitTests.Features;

public sealed class EquipmentServiceTests
{
    private static EquipmentService NewEquipmentService(
        AppDbContext db,
        FakeAuditService audit,
        FixedDateTimeProvider clock) =>
        new(db, audit, clock);

    private static async Task<Client> AddClientAsync(
        AppDbContext db,
        string code = "CLI-00001",
        string? name = null)
    {
        var client = new Client
        {
            Code = code,
            Name = name ?? $"Cliente {code}",
            IsActive = true,
        };

        db.Clients.Add(client);
        await db.SaveChangesAsync();
        return client;
    }

    private static async Task<InventoryItem> AddItemAsync(
        AppDbContext db,
        string code = "ART-00001",
        string name = "Compresor hermético",
        string? brand = "Bitzer",
        string? model = "4FC-3.2",
        bool isActive = true)
    {
        var item = new InventoryItem
        {
            Code = code,
            Name = name,
            Category = InventoryCategory.Equipment,
            Unit = InventoryUnit.Unit,
            Brand = brand,
            Model = model,
            QuantityOnHand = 5m,
            AverageUnitCost = 850m,
            MinimumStock = 0m,
            IsActive = isActive,
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static async Task<Installation> AddInstallationAsync(
        AppDbContext db,
        Client client,
        string code = "INS-00001",
        InstallationStatus status = InstallationStatus.Completed,
        DateOnly? completedDate = null,
        string name = "Instalación de equipos vendidos")
    {
        var installation = new Installation
        {
            Code = code,
            ClientId = client.Id,
            Client = client,
            Name = name,
            Location = "Planta principal",
            Status = status,
            CompletedDate = status == InstallationStatus.Completed
                ? completedDate ?? new DateOnly(2026, 11, 10)
                : completedDate,
        };

        db.Installations.Add(installation);
        await db.SaveChangesAsync();
        return installation;
    }

    private static CreateEquipmentRequest CreateRequest(
        Installation installation,
        InventoryItem item,
        string? name = null,
        string? serialNumber = "SN-0001",
        DateOnly? installedDate = null,
        DateOnly? warrantyEndDate = null,
        string? location = "Sala de máquinas") =>
        new(
            installation.Id,
            item.Id,
            name,
            serialNumber,
            null,
            null,
            location,
            installedDate,
            warrantyEndDate,
            null);

    [Fact]
    public async Task CreateEquipment_OnCompletedInstallation_GeneratesCodeAndInitialHistory()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var equipment = NewEquipmentService(db, audit, clock);

        var client = await AddClientAsync(db, name: "Frío del Norte SAC");
        var item = await AddItemAsync(db, brand: "Bitzer", model: "4FC-3.2");
        var installation = await AddInstallationAsync(db, client);

        var created = await equipment.CreateAsync(
            CreateRequest(
                installation,
                item,
                serialNumber: "  sn-0001  ",
                warrantyEndDate: new DateOnly(2027, 11, 10)),
            CancellationToken.None);

        Assert.Equal("EQP-00001", created.Code);
        Assert.Equal(client.Id, created.ClientId);
        Assert.Equal("Frío del Norte SAC", created.ClientName);
        Assert.Equal(installation.Id, created.InstallationId);
        Assert.Equal(installation.Code, created.InstallationCode);
        Assert.Equal(item.Id, created.InventoryItemId);
        Assert.Equal(item.Code, created.ItemCode);
        Assert.Equal("Compresor hermético (4FC-3.2)", created.Name);
        Assert.Equal("SN-0001", created.SerialNumber);
        Assert.Equal("Bitzer", created.Brand);
        Assert.Equal("4FC-3.2", created.Model);
        Assert.Equal("Sala de máquinas", created.Location);
        Assert.Equal(new DateOnly(2026, 11, 10), created.InstalledDate);
        Assert.Equal(new DateOnly(2027, 11, 10), created.WarrantyEndDate);
        Assert.True(created.IsUnderWarranty);
        Assert.Equal(EquipmentStatus.Operational, created.Status);
        Assert.Contains("equipment.created", audit.Actions);

        var history = await equipment.GetHistoryAsync(created.Id, CancellationToken.None);

        var initial = Assert.Single(history);
        Assert.Null(initial.FromStatus);
        Assert.Equal(EquipmentStatus.Operational, initial.ToStatus);
        Assert.Equal(new DateOnly(2026, 11, 10), initial.ChangedDate);
        Assert.Contains("Registro inicial", initial.Reason);
    }

    [Fact]
    public async Task CreateEquipment_RequiresCompletedInstallationAndActiveItem()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var equipment = NewEquipmentService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var pending = await AddInstallationAsync(
            db,
            client,
            code: "INS-00002",
            status: InstallationStatus.Scheduled,
            completedDate: null);

        await Assert.ThrowsAsync<ConflictException>(() =>
            equipment.CreateAsync(
                CreateRequest(pending, item),
                CancellationToken.None));

        var inactiveItem = await AddItemAsync(
            db,
            code: "ART-00002",
            name: "Compresor en desuso",
            isActive: false);

        var completed = await AddInstallationAsync(db, client);

        var inactive = await Assert.ThrowsAsync<ConflictException>(() =>
            equipment.CreateAsync(
                CreateRequest(completed, inactiveItem),
                CancellationToken.None));

        Assert.Contains("inactivo", inactive.Message);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            equipment.CreateAsync(
                new CreateEquipmentRequest(
                    Guid.NewGuid(), item.Id, null, null, null, null, null, null, null, null),
                CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            equipment.CreateAsync(
                new CreateEquipmentRequest(
                    completed.Id, Guid.NewGuid(), null, null, null, null, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateEquipment_RejectsDuplicatedSerialAndInvalidDates()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var equipment = NewEquipmentService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var otherItem = await AddItemAsync(db, code: "ART-00002", name: "Evaporador");
        var installation = await AddInstallationAsync(db, client);

        var first = await equipment.CreateAsync(
            CreateRequest(installation, item, serialNumber: "AB-100"),
            CancellationToken.None);

        Assert.Equal("AB-100", first.SerialNumber);

        var duplicated = await Assert.ThrowsAsync<ConflictException>(() =>
            equipment.CreateAsync(
                CreateRequest(installation, item, serialNumber: " ab-100 "),
                CancellationToken.None));

        Assert.Contains("número de serie", duplicated.Message);

        var sameSerialOtherItem = await equipment.CreateAsync(
            CreateRequest(installation, otherItem, serialNumber: "ab-100"),
            CancellationToken.None);

        Assert.Equal("AB-100", sameSerialOtherItem.SerialNumber);
        Assert.Equal("EQP-00002", sameSerialOtherItem.Code);

        var futureDate = await Assert.ThrowsAsync<ValidationAppException>(() =>
            equipment.CreateAsync(
                CreateRequest(
                    installation,
                    item,
                    serialNumber: null,
                    installedDate: new DateOnly(2026, 12, 1)),
                CancellationToken.None));

        Assert.Contains("InstalledDate", futureDate.Errors.Keys);

        var badWarranty = await Assert.ThrowsAsync<ValidationAppException>(() =>
            equipment.CreateAsync(
                CreateRequest(
                    installation,
                    item,
                    serialNumber: null,
                    installedDate: new DateOnly(2026, 11, 10),
                    warrantyEndDate: new DateOnly(2026, 11, 9)),
                CancellationToken.None));

        Assert.Contains("WarrantyEndDate", badWarranty.Errors.Keys);
    }

    [Fact]
    public async Task UpdateEquipment_ChangesDetailsAndProtectsSerialAndRetired()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var equipment = NewEquipmentService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var installation = await AddInstallationAsync(db, client);

        var first = await equipment.CreateAsync(
            CreateRequest(installation, item, serialNumber: "SN-0001"),
            CancellationToken.None);

        var second = await equipment.CreateAsync(
            CreateRequest(installation, item, serialNumber: "SN-0002"),
            CancellationToken.None);

        var updated = await equipment.UpdateAsync(
            first.Id,
            new UpdateEquipmentRequest(
                "Compresor de baja temperatura",
                "sn-0001-b",
                "Copeland",
                "ZS45K4E",
                "Cuarto frío 1",
                new DateOnly(2028, 5, 1),
                "Equipo reubicado"),
            CancellationToken.None);

        Assert.Equal("Compresor de baja temperatura", updated.Name);
        Assert.Equal("SN-0001-B", updated.SerialNumber);
        Assert.Equal("Copeland", updated.Brand);
        Assert.Equal("ZS45K4E", updated.Model);
        Assert.Equal("Cuarto frío 1", updated.Location);
        Assert.Equal(new DateOnly(2028, 5, 1), updated.WarrantyEndDate);
        Assert.Equal("Equipo reubicado", updated.Notes);
        Assert.Contains("equipment.updated", audit.Actions);

        await Assert.ThrowsAsync<ConflictException>(() =>
            equipment.UpdateAsync(
                first.Id,
                new UpdateEquipmentRequest(
                    "Compresor", second.SerialNumber, null, null, null, null, null),
                CancellationToken.None));

        var badWarranty = await Assert.ThrowsAsync<ValidationAppException>(() =>
            equipment.UpdateAsync(
                first.Id,
                new UpdateEquipmentRequest(
                    "Compresor", "SN-0001-B", null, null, null,
                    new DateOnly(2026, 11, 1), null),
                CancellationToken.None));

        Assert.Contains("WarrantyEndDate", badWarranty.Errors.Keys);

        await equipment.ChangeStatusAsync(
            first.Id,
            new ChangeEquipmentStatusRequest(
                EquipmentStatus.Retired,
                new DateOnly(2026, 11, 12),
                "Equipo obsoleto"),
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            equipment.UpdateAsync(
                first.Id,
                new UpdateEquipmentRequest(
                    "No permitido", "SN-0001-B", null, null, null, null, null),
                CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            equipment.UpdateAsync(
                Guid.NewGuid(),
                new UpdateEquipmentRequest("Otro", null, null, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task ChangeStatus_TracksHistory_AndBlocksRetiredEquipment()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var equipment = NewEquipmentService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var installation = await AddInstallationAsync(db, client);

        var created = await equipment.CreateAsync(
            CreateRequest(installation, item, serialNumber: "SN-0100"),
            CancellationToken.None);

        var maintenance = await equipment.ChangeStatusAsync(
            created.Id,
            new ChangeEquipmentStatusRequest(
                EquipmentStatus.UnderMaintenance,
                new DateOnly(2026, 11, 11),
                "Mantenimiento preventivo"),
            CancellationToken.None);

        Assert.Equal(EquipmentStatus.UnderMaintenance, maintenance.Status);

        var repeated = await Assert.ThrowsAsync<ConflictException>(() =>
            equipment.ChangeStatusAsync(
                created.Id,
                new ChangeEquipmentStatusRequest(
                    EquipmentStatus.UnderMaintenance,
                    null,
                    "Sin cambio real"),
                CancellationToken.None));

        Assert.Contains("ya está en estado", repeated.Message);

        var outOfService = await equipment.ChangeStatusAsync(
            created.Id,
            new ChangeEquipmentStatusRequest(
                EquipmentStatus.OutOfService,
                new DateOnly(2026, 11, 12),
                "Falla del compresor"),
            CancellationToken.None);

        Assert.Equal(EquipmentStatus.OutOfService, outOfService.Status);

        var retired = await equipment.ChangeStatusAsync(
            created.Id,
            new ChangeEquipmentStatusRequest(
                EquipmentStatus.Retired,
                new DateOnly(2026, 11, 12),
                "Baja definitiva"),
            CancellationToken.None);

        Assert.Equal(EquipmentStatus.Retired, retired.Status);

        await Assert.ThrowsAsync<ConflictException>(() =>
            equipment.ChangeStatusAsync(
                created.Id,
                new ChangeEquipmentStatusRequest(
                    EquipmentStatus.Operational,
                    null,
                    "Reactivación"),
                CancellationToken.None));

        var history = await equipment.GetHistoryAsync(created.Id, CancellationToken.None);

        Assert.Equal(4, history.Count);
        Assert.Contains(history, change => change.ToStatus == EquipmentStatus.UnderMaintenance);
        Assert.Contains(history, change => change.ToStatus == EquipmentStatus.OutOfService);
        Assert.Contains(history, change => change.ToStatus == EquipmentStatus.Retired);

        var initial = history[^1];
        Assert.Null(initial.FromStatus);
        Assert.Equal(EquipmentStatus.Operational, initial.ToStatus);
        Assert.Contains(
            history,
            change => change.FromStatus == EquipmentStatus.Operational
                && change.ToStatus == EquipmentStatus.UnderMaintenance);
        Assert.Contains("equipment.status_changed", audit.Actions);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            equipment.GetHistoryAsync(Guid.NewGuid(), CancellationToken.None));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            equipment.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ChangeStatus_ValidatesReasonAndDates()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var equipment = NewEquipmentService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var installation = await AddInstallationAsync(db, client);

        var created = await equipment.CreateAsync(
            CreateRequest(installation, item),
            CancellationToken.None);

        var noReason = await Assert.ThrowsAsync<ValidationAppException>(() =>
            equipment.ChangeStatusAsync(
                created.Id,
                new ChangeEquipmentStatusRequest(
                    EquipmentStatus.UnderMaintenance,
                    null,
                    "   "),
                CancellationToken.None));

        Assert.Contains("Reason", noReason.Errors.Keys);

        var futureDate = await Assert.ThrowsAsync<ValidationAppException>(() =>
            equipment.ChangeStatusAsync(
                created.Id,
                new ChangeEquipmentStatusRequest(
                    EquipmentStatus.UnderMaintenance,
                    new DateOnly(2026, 12, 1),
                    "Programado"),
                CancellationToken.None));

        Assert.Contains("ChangedDate", futureDate.Errors.Keys);

        var beforeInstall = await Assert.ThrowsAsync<ValidationAppException>(() =>
            equipment.ChangeStatusAsync(
                created.Id,
                new ChangeEquipmentStatusRequest(
                    EquipmentStatus.UnderMaintenance,
                    new DateOnly(2026, 11, 1),
                    "Fecha inválida"),
                CancellationToken.None));

        Assert.Contains("ChangedDate", beforeInstall.Errors.Keys);
    }

    [Fact]
    public async Task EquipmentQuery_FiltersByStatusClientWarrantyAndSearch()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var equipment = NewEquipmentService(db, audit, clock);

        var north = await AddClientAsync(db, "CLI-00001", "Frío del Norte SAC");
        var south = await AddClientAsync(db, "CLI-00002", "Clima Sur EIRL");

        var compressor = await AddItemAsync(db, "ART-00001", "Compresor hermético");
        var evaporator = await AddItemAsync(
            db, "ART-00002", "Evaporador de aire", brand: "Danfoss", model: "EC-25");

        var northInstallation = await AddInstallationAsync(
            db,
            north,
            code: "INS-00001",
            completedDate: new DateOnly(2026, 11, 10));

        var southInstallation = await AddInstallationAsync(
            db,
            south,
            code: "INS-00002",
            completedDate: new DateOnly(2026, 10, 1));

        var underWarranty = await equipment.CreateAsync(
            CreateRequest(
                northInstallation,
                compressor,
                name: "Compresor principal",
                serialNumber: "SERIE-NORTE",
                installedDate: new DateOnly(2026, 11, 10),
                warrantyEndDate: new DateOnly(2027, 11, 10)),
            CancellationToken.None);

        var expired = await equipment.CreateAsync(
            CreateRequest(
                southInstallation,
                evaporator,
                name: "Evaporador secundario",
                serialNumber: "SERIE-SUR",
                installedDate: new DateOnly(2026, 10, 1),
                warrantyEndDate: null),
            CancellationToken.None);

        await equipment.ChangeStatusAsync(
            expired.Id,
            new ChangeEquipmentStatusRequest(
                EquipmentStatus.UnderMaintenance,
                new DateOnly(2026, 11, 1),
                "Limpieza programada"),
            CancellationToken.None);

        var all = await equipment.GetPagedAsync(
            new EquipmentQuery { SortDesc = true },
            CancellationToken.None);

        Assert.Equal(2, all.TotalItems);
        Assert.Equal(underWarranty.Id, all.Items[0].Id);

        var maintenance = await equipment.GetPagedAsync(
            new EquipmentQuery { Status = EquipmentStatus.UnderMaintenance },
            CancellationToken.None);

        Assert.Single(maintenance.Items);
        Assert.Equal(expired.Id, maintenance.Items[0].Id);

        var byClient = await equipment.GetPagedAsync(
            new EquipmentQuery { ClientId = north.Id },
            CancellationToken.None);

        Assert.Single(byClient.Items);
        Assert.Equal(underWarranty.Id, byClient.Items[0].Id);

        var byInstallation = await equipment.GetPagedAsync(
            new EquipmentQuery { InstallationId = southInstallation.Id },
            CancellationToken.None);

        Assert.Single(byInstallation.Items);

        var byItem = await equipment.GetPagedAsync(
            new EquipmentQuery { InventoryItemId = compressor.Id },
            CancellationToken.None);

        Assert.Single(byItem.Items);

        var inWarranty = await equipment.GetPagedAsync(
            new EquipmentQuery { UnderWarranty = true },
            CancellationToken.None);

        Assert.Single(inWarranty.Items);
        Assert.Equal(underWarranty.Id, inWarranty.Items[0].Id);

        var outOfWarranty = await equipment.GetPagedAsync(
            new EquipmentQuery { UnderWarranty = false },
            CancellationToken.None);

        Assert.Single(outOfWarranty.Items);
        Assert.Equal(expired.Id, outOfWarranty.Items[0].Id);

        var search = await equipment.GetPagedAsync(
            new EquipmentQuery { Search = "SERIE-NORTE" },
            CancellationToken.None);

        Assert.Single(search.Items);

        var searchClient = await equipment.GetPagedAsync(
            new EquipmentQuery { Search = "Clima Sur" },
            CancellationToken.None);

        Assert.Single(searchClient.Items);

        var searchItem = await equipment.GetPagedAsync(
            new EquipmentQuery { Search = "Evaporador" },
            CancellationToken.None);

        Assert.Single(searchItem.Items);

        var range = await equipment.GetPagedAsync(
            new EquipmentQuery
            {
                FromDate = new DateOnly(2026, 11, 1),
                ToDate = new DateOnly(2026, 11, 30),
            },
            CancellationToken.None);

        Assert.Single(range.Items);

        var invalidRange = await Assert.ThrowsAsync<ValidationAppException>(() =>
            equipment.GetPagedAsync(
                new EquipmentQuery
                {
                    FromDate = new DateOnly(2026, 11, 30),
                    ToDate = new DateOnly(2026, 11, 1),
                },
                CancellationToken.None));

        Assert.Contains("FromDate", invalidRange.Errors.Keys);

        var sorted = await equipment.GetPagedAsync(
            new EquipmentQuery { SortBy = "name", SortDesc = false },
            CancellationToken.None);

        Assert.Equal("Compresor principal", sorted.Items[0].Name);
        Assert.Equal("Evaporador secundario", sorted.Items[1].Name);
    }

    [Fact]
    public void Validators_RejectInvalidEquipmentRequests()
    {
        var createValidator = new CreateEquipmentRequestValidator();

        var invalidCreate = createValidator.Validate(new CreateEquipmentRequest(
            Guid.Empty,
            Guid.Empty,
            new string('x', 201),
            new string('x', 101),
            new string('x', 101),
            new string('x', 101),
            new string('x', 301),
            null,
            null,
            new string('x', 2001)));

        Assert.False(invalidCreate.IsValid);
        Assert.Contains(invalidCreate.Errors, error => error.PropertyName == "InstallationId");
        Assert.Contains(invalidCreate.Errors, error => error.PropertyName == "InventoryItemId");
        Assert.Contains(invalidCreate.Errors, error => error.PropertyName == "Name");
        Assert.Contains(invalidCreate.Errors, error => error.PropertyName == "SerialNumber");
        Assert.Contains(invalidCreate.Errors, error => error.PropertyName == "Location");

        var validCreate = createValidator.Validate(new CreateEquipmentRequest(
            Guid.NewGuid(), Guid.NewGuid(), null, "SN-1", "Bitzer", "4FC", null, null, null, null));

        Assert.True(validCreate.IsValid);

        var updateValidator = new UpdateEquipmentRequestValidator();

        var invalidUpdate = updateValidator.Validate(new UpdateEquipmentRequest(
            " ",
            null,
            null,
            null,
            new string('x', 301),
            null,
            null));

        Assert.False(invalidUpdate.IsValid);
        Assert.Contains(invalidUpdate.Errors, error => error.PropertyName == "Name");
        Assert.Contains(invalidUpdate.Errors, error => error.PropertyName == "Location");

        var statusValidator = new ChangeEquipmentStatusRequestValidator();

        var missingReason = statusValidator.Validate(
            new ChangeEquipmentStatusRequest(EquipmentStatus.UnderMaintenance, null, " "));

        Assert.False(missingReason.IsValid);
        Assert.Contains(missingReason.Errors, error => error.PropertyName == "Reason");

        var invalidStatus = statusValidator.Validate(
            new ChangeEquipmentStatusRequest((EquipmentStatus)99, null, "Cambio"));

        Assert.False(invalidStatus.IsValid);
        Assert.Contains(invalidStatus.Errors, error => error.PropertyName == "Status");

        var validStatus = statusValidator.Validate(
            new ChangeEquipmentStatusRequest(
                EquipmentStatus.OutOfService,
                new DateOnly(2026, 11, 12),
                "Falla"));

        Assert.True(validStatus.IsValid);
    }
}
