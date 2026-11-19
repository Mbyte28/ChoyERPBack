using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Features.Installations;
using ChoyERP.Application.Features.Installations.Dtos;
using ChoyERP.Application.Features.Sales;
using ChoyERP.Application.Features.Sales.Dtos;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Installations;
using ChoyERP.Domain.Entities.Inventory;
using ChoyERP.Domain.Entities.Sales;
using ChoyERP.Domain.Entities.Workers;
using ChoyERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ChoyERP.UnitTests.Features;

public sealed class InstallationServiceTests
{
    private static InstallationService NewInstallationService(
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

    private static async Task<ClientAddress> AddAddressAsync(
        AppDbContext db,
        Guid clientId,
        string label = "Planta principal")
    {
        var address = new ClientAddress
        {
            ClientId = clientId,
            Label = label,
            Line1 = "Av. Industrial 1250",
            City = "Lima",
            Country = "Perú",
            IsPrimary = true,
        };

        db.ClientAddresses.Add(address);
        await db.SaveChangesAsync();
        return address;
    }

    private static async Task<InventoryItem> AddItemAsync(AppDbContext db)
    {
        var item = new InventoryItem
        {
            Code = "ART-00001",
            Name = "Compresor hermético",
            Category = InventoryCategory.Equipment,
            Unit = InventoryUnit.Unit,
            QuantityOnHand = 10m,
            AverageUnitCost = 80m,
            MinimumStock = 0m,
        };

        db.InventoryItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static async Task<Worker> AddWorkerAsync(
        AppDbContext db,
        bool isActive = true)
    {
        var worker = new Worker
        {
            FirstName = "Luis",
            LastName = "Ramírez",
            DocumentNumber = "12345678",
            IsActive = isActive,
        };

        db.Workers.Add(worker);
        await db.SaveChangesAsync();
        return worker;
    }

    private static async Task<SaleDto> AddSaleAsync(
        AppDbContext db,
        FakeAuditService audit,
        FixedDateTimeProvider clock,
        Guid clientId,
        Guid itemId,
        bool confirm = true,
        bool deliver = false)
    {
        var sales = new SaleService(db, audit, clock);

        var sale = await sales.CreateAsync(
            new CreateSaleRequest(
                clientId,
                null,
                "PEN",
                0.18m,
                null,
                null,
                null,
                [new SaleItemRequest(itemId, 1m, 150m)]),
            CancellationToken.None);

        if (confirm)
        {
            sale = await sales.ConfirmAsync(sale.Id, CancellationToken.None);
        }

        if (deliver)
        {
            sale = await sales.DeliverAsync(
                sale.Id,
                new DeliverSaleRequest(null, null),
                CancellationToken.None);
        }

        return sale;
    }

    [Fact]
    public async Task CreateInstallation_ForSale_DerivesClientAndDefaults()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var installations = NewInstallationService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var worker = await AddWorkerAsync(db);
        var address = await AddAddressAsync(db, client.Id);
        var sale = await AddSaleAsync(db, audit, clock, client.Id, item.Id);

        var installation = await installations.CreateAsync(
            new CreateInstallationRequest(
                sale.Id,
                null,
                address.Id,
                null,
                "Instalación de equipos vendidos",
                null,
                new DateOnly(2026, 11, 15),
                worker.Id,
                "Llevar herramientas de vacío"),
            CancellationToken.None);

        Assert.Equal("INS-00001", installation.Code);
        Assert.Equal(client.Id, installation.ClientId);
        Assert.Equal(sale.Id, installation.SaleId);
        Assert.Equal(sale.Code, installation.SaleCode);
        Assert.Null(installation.ProjectId);
        Assert.Equal("Instalación de equipos vendidos", installation.Name);
        Assert.Equal(InstallationStatus.Scheduled, installation.Status);
        Assert.Equal("Av. Industrial 1250, Lima, Perú", installation.Address);
        Assert.Equal(worker.Id, installation.TechnicianWorkerId);
        Assert.Equal("Luis Ramírez", installation.TechnicianName);
        Assert.Equal(new DateOnly(2026, 11, 15), installation.ScheduledDate);
        Assert.Contains("installations.created", audit.Actions);

        await Assert.ThrowsAsync<ConflictException>(() =>
            installations.CreateAsync(
                new CreateInstallationRequest(
                    sale.Id, null, null, null, null, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateInstallation_ForProject_UsesProjectLocation_AndRequiresConfirmedSale()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var installations = NewInstallationService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var address = await AddAddressAsync(db, client.Id);

        var project = new Project
        {
            Code = "PRY-00001",
            ContractId = Guid.NewGuid(),
            ClientId = client.Id,
            ClientAddressId = address.Id,
            Name = "Cámara frigorífica",
            Status = ProjectStatus.InProgress,
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var fromProject = await installations.CreateAsync(
            new CreateInstallationRequest(
                null, project.Id, null, "Planta norte", null, null, null, null, null),
            CancellationToken.None);

        Assert.Equal("INS-00001", fromProject.Code);
        Assert.Equal(project.Id, fromProject.ProjectId);
        Assert.Equal("Instalación de proyecto PRY-00001", fromProject.Name);
        Assert.Equal("Planta norte", fromProject.Location);

        var secondProject = new Project
        {
            Code = "PRY-00002",
            ContractId = Guid.NewGuid(),
            ClientId = client.Id,
            ClientAddressId = address.Id,
            Name = "Segunda cámara",
            Status = ProjectStatus.Planned,
        };

        db.Projects.Add(secondProject);
        await db.SaveChangesAsync();

        var defaultLocation = await installations.CreateAsync(
            new CreateInstallationRequest(
                null, secondProject.Id, address.Id, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.Equal("Av. Industrial 1250, Lima, Perú", defaultLocation.Location);

        var draftSale = await AddSaleAsync(db, audit, clock, client.Id, item.Id, confirm: false);

        await Assert.ThrowsAsync<ConflictException>(() =>
            installations.CreateAsync(
                new CreateInstallationRequest(
                    draftSale.Id, null, null, null, null, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateInstallation_ValidatesOriginAddressAndWorker()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var installations = NewInstallationService(db, audit, clock);

        var client = await AddClientAsync(db);
        var otherClient = await AddClientAsync(db, "CLI-00002");
        var item = await AddItemAsync(db);
        var worker = await AddWorkerAsync(db);
        var foreignAddress = await AddAddressAsync(db, otherClient.Id, "Otra planta");
        var sale = await AddSaleAsync(db, audit, clock, client.Id, item.Id);

        var noOrigin = await Assert.ThrowsAsync<ValidationAppException>(() =>
            installations.CreateAsync(
                new CreateInstallationRequest(
                    null, null, null, null, null, null, null, null, null),
                CancellationToken.None));

        Assert.Contains("SaleId", noOrigin.Errors.Keys);

        var bothOrigins = await Assert.ThrowsAsync<ValidationAppException>(() =>
            installations.CreateAsync(
                new CreateInstallationRequest(
                    sale.Id, Guid.NewGuid(), null, null, null, null, null, null, null),
                CancellationToken.None));

        Assert.Contains("SaleId", bothOrigins.Errors.Keys);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            installations.CreateAsync(
                new CreateInstallationRequest(
                    sale.Id, null, foreignAddress.Id, null, null, null, null, null, null),
                CancellationToken.None));

        worker.IsActive = false;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            installations.CreateAsync(
                new CreateInstallationRequest(
                    sale.Id, null, null, null, null, null, null, worker.Id, null),
                CancellationToken.None));

        client.IsActive = false;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await Assert.ThrowsAsync<ConflictException>(() =>
            installations.CreateAsync(
                new CreateInstallationRequest(
                    sale.Id, null, null, null, null, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task InstallationLifecycle_SchedulesStartsAndCompletes()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var installations = NewInstallationService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var sale = await AddSaleAsync(db, audit, clock, client.Id, item.Id, deliver: true);

        var installation = await installations.CreateAsync(
            new CreateInstallationRequest(
                sale.Id, null, null, "Planta principal", null, null,
                new DateOnly(2026, 11, 11), null, null),
            CancellationToken.None);

        var started = await installations.StartAsync(
            installation.Id,
            new StartInstallationRequest(null),
            CancellationToken.None);

        Assert.Equal(InstallationStatus.InProgress, started.Status);
        Assert.Equal(new DateOnly(2026, 11, 12), started.StartedDate);

        await Assert.ThrowsAsync<ConflictException>(() =>
            installations.StartAsync(
                installation.Id,
                new StartInstallationRequest(null),
                CancellationToken.None));

        var updated = await installations.UpdateAsync(
            installation.Id,
            new UpdateInstallationRequest(
                null,
                "Planta principal - zona de carga",
                "Instalación de equipos vendidos",
                null,
                new DateOnly(2026, 11, 13),
                null,
                "Reprogramada por lluvia"),
            CancellationToken.None);

        Assert.Equal("Planta principal - zona de carga", updated.Location);

        var completed = await installations.CompleteAsync(
            installation.Id,
            new CompleteInstallationRequest(null, "Equipo instalado y probado"),
            CancellationToken.None);

        Assert.Equal(InstallationStatus.Completed, completed.Status);
        Assert.Equal(new DateOnly(2026, 11, 12), completed.CompletedDate);
        Assert.Equal("Equipo instalado y probado", completed.Notes);
        Assert.Contains("installations.started", audit.Actions);
        Assert.Contains("installations.completed", audit.Actions);

        var storedSale = await db.Sales.SingleAsync(s => s.Id == sale.Id);
        Assert.Equal(SaleStatus.Completed, storedSale.Status);

        await Assert.ThrowsAsync<ConflictException>(() =>
            installations.CompleteAsync(
                installation.Id,
                new CompleteInstallationRequest(null, null),
                CancellationToken.None));

        await Assert.ThrowsAsync<ConflictException>(() =>
            installations.CancelAsync(installation.Id, "Ya no aplica", CancellationToken.None));

        await Assert.ThrowsAsync<ConflictException>(() =>
            installations.UpdateAsync(
                installation.Id,
                new UpdateInstallationRequest(
                    null, null, "Otro nombre", null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task InstallationDates_AreValidated_AndCancellationKeepsReason()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var installations = NewInstallationService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var sale = await AddSaleAsync(db, audit, clock, client.Id, item.Id);

        var installation = await installations.CreateAsync(
            new CreateInstallationRequest(
                sale.Id, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        var futureStart = await Assert.ThrowsAsync<ValidationAppException>(() =>
            installations.StartAsync(
                installation.Id,
                new StartInstallationRequest(new DateOnly(2026, 12, 1)),
                CancellationToken.None));

        Assert.Contains("StartedDate", futureStart.Errors.Keys);

        await installations.StartAsync(
            installation.Id,
            new StartInstallationRequest(new DateOnly(2026, 11, 11)),
            CancellationToken.None);

        var beforeStart = await Assert.ThrowsAsync<ValidationAppException>(() =>
            installations.CompleteAsync(
                installation.Id,
                new CompleteInstallationRequest(new DateOnly(2026, 11, 10), null),
                CancellationToken.None));

        Assert.Contains("CompletedDate", beforeStart.Errors.Keys);

        var cancelled = await installations.CancelAsync(
            installation.Id,
            "El cliente reprogramó la obra",
            CancellationToken.None);

        Assert.Equal(InstallationStatus.Cancelled, cancelled.Status);
        Assert.Equal("El cliente reprogramó la obra", cancelled.CancellationReason);
        Assert.Contains("installations.cancelled", audit.Actions);

        await Assert.ThrowsAsync<ConflictException>(() =>
            installations.CancelAsync(installation.Id, "Dos veces", CancellationToken.None));
    }

    [Fact]
    public async Task InstallationQuery_FiltersByOriginStatusAndSearch()
    {
        var db = TestDb.Create();
        var audit = new FakeAuditService();
        var clock = new FixedDateTimeProvider();
        var installations = NewInstallationService(db, audit, clock);

        var client = await AddClientAsync(db);
        var item = await AddItemAsync(db);
        var sale = await AddSaleAsync(db, audit, clock, client.Id, item.Id);

        var project = new Project
        {
            Code = "PRY-00001",
            ContractId = Guid.NewGuid(),
            ClientId = client.Id,
            Name = "Túnel de congelamiento",
            Status = ProjectStatus.InProgress,
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var fromSale = await installations.CreateAsync(
            new CreateInstallationRequest(
                sale.Id, null, null, "Planta sur", "Instalación de venta", null,
                new DateOnly(2026, 11, 20), null, null),
            CancellationToken.None);

        var fromProject = await installations.CreateAsync(
            new CreateInstallationRequest(
                null, project.Id, null, "Planta norte", "Instalación de proyecto", null,
                new DateOnly(2026, 11, 25), null, null),
            CancellationToken.None);

        await installations.CancelAsync(
            fromProject.Id,
            "Proyecto reprogramado",
            CancellationToken.None);

        var scheduled = await installations.GetPagedAsync(
            new InstallationQuery { Status = InstallationStatus.Scheduled },
            CancellationToken.None);

        Assert.Single(scheduled.Items);
        Assert.Equal(fromSale.Id, scheduled.Items[0].Id);

        var bySale = await installations.GetPagedAsync(
            new InstallationQuery { SaleId = sale.Id },
            CancellationToken.None);

        Assert.Single(bySale.Items);

        var byProject = await installations.GetPagedAsync(
            new InstallationQuery { ProjectId = project.Id },
            CancellationToken.None);

        Assert.Single(byProject.Items);

        var search = await installations.GetPagedAsync(
            new InstallationQuery { Search = "PRY-00001" },
            CancellationToken.None);

        Assert.Single(search.Items);
        Assert.Equal(fromProject.Id, search.Items[0].Id);

        Assert.Equal(2, (await installations.GetPagedAsync(
            new InstallationQuery(),
            CancellationToken.None)).TotalItems);
    }
}
