using ChoyERP.Application.Common.Exceptions;
using ChoyERP.Application.Features.Contracting;
using ChoyERP.Application.Features.Contracting.Dtos;
using ChoyERP.Application.Features.Contracting.Validators;
using ChoyERP.Domain.Entities.Clients;
using ChoyERP.Domain.Entities.Contracting;
using ChoyERP.Domain.Entities.Workers;
using ChoyERP.Infrastructure.Persistence;

namespace ChoyERP.UnitTests.Features;

public sealed class ContractingServiceTests
{
    private static async Task<(AppDbContext Db, Client Client, Worker Worker)> SeedAsync()
    {
        var db = TestDb.Create();

        var client = new Client
        {
            Code = "CLI-00001",
            Name = "Corporación Andina",
            TaxId = "20123456789",
            IsActive = true,
        };

        client.Addresses.Add(new ClientAddress
        {
            Label = "Planta principal",
            Line1 = "Av. Industrial 1250",
            City = "Lima",
            Country = "Perú",
            IsPrimary = true,
        });

        var worker = new Worker
        {
            FirstName = "María",
            LastName = "Torres",
            DocumentNumber = "23456789",
            Position = "Ingeniera de proyectos",
            IsActive = true,
        };

        db.Clients.Add(client);
        db.Workers.Add(worker);
        await db.SaveChangesAsync();

        return (db, client, worker);
    }

    private static CreateProjectRequestRequest NewRequest(Client client, Guid? addressId = null) =>
        new(
            client.Id,
            addressId,
            "Cámara frigorífica 20 m3",
            "Instalación en planta del cliente",
            ProjectType.ColdRoom,
            RequestPriority.High,
            null,
            null,
            null);

    private static CreateQuoteRequest NewQuote(Guid requestId) =>
        new(
            requestId,
            "PEN",
            0.18m,
            new DateOnly(2026, 12, 31),
            null,
            [
                new QuoteItemRequest("Equipo frigorífico", "UND", 2, 1000m),
                new QuoteItemRequest("Instalación y puesta en marcha", "GLB", 1, 500m),
            ]);

    [Fact]
    public async Task FullFlow_FromRequestToCompletedProject()
    {
        var (db, client, worker) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);
        var contracts = new ContractService(db, audit, clock);
        var projects = new ProjectService(db, audit, clock);

        var addressId = client.Addresses.Single().Id;

        var request = await requests.CreateAsync(
            NewRequest(client, addressId),
            CancellationToken.None);

        Assert.Equal("SOL-00001", request.Code);
        Assert.Equal(ProjectRequestStatus.Received, request.Status);
        Assert.Equal(new DateOnly(2026, 11, 12), request.RequestedAt);

        var quote = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);

        Assert.Equal("COT-00001", quote.Code);
        Assert.Equal(1, quote.Version);
        Assert.Equal(2500m, quote.Subtotal);
        Assert.Equal(450m, quote.TaxAmount);
        Assert.Equal(2950m, quote.Total);
        Assert.Equal(QuoteStatus.Draft, quote.Status);

        var sent = await quotes.SendAsync(quote.Id, new SendQuoteRequest(null), CancellationToken.None);
        Assert.Equal(QuoteStatus.Sent, sent.Status);

        var requestAfterSend = await requests.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(ProjectRequestStatus.Quoted, requestAfterSend.Status);

        var accepted = await quotes.AcceptAsync(quote.Id, CancellationToken.None);
        Assert.Equal(QuoteStatus.Accepted, accepted.Status);
        Assert.NotNull(accepted.AcceptedAt);

        var requestAfterAccept = await requests.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(ProjectRequestStatus.Accepted, requestAfterAccept.Status);

        var contract = await contracts.CreateAsync(
            new CreateContractRequest(
                quote.Id,
                new DateOnly(2026, 11, 12),
                new DateOnly(2026, 11, 20),
                null,
                null),
            CancellationToken.None);

        Assert.Equal("CTR-00001", contract.Code);
        Assert.Equal(2950m, contract.Amount);
        Assert.Equal(ContractStatus.Draft, contract.Status);

        var activeContract = await contracts.ChangeStatusAsync(
            contract.Id,
            ContractStatus.Active,
            CancellationToken.None);

        Assert.Equal(ContractStatus.Active, activeContract.Status);
        Assert.NotNull(activeContract.SignedAt);

        var project = await projects.CreateAsync(
            new CreateProjectRequest(
                contract.Id,
                null,
                "Proyecto de instalación",
                null,
                new DateOnly(2027, 1, 31),
                worker.Id,
                null),
            CancellationToken.None);

        Assert.Equal("PRY-00001", project.Code);
        Assert.Equal("Cámara frigorífica 20 m3", project.Name);
        Assert.Contains("Av. Industrial 1250", project.Location);
        Assert.Equal(worker.Id, project.ResponsibleWorkerId);
        Assert.Equal(ProjectStatus.Planned, project.Status);

        var inProgress = await projects.ChangeStatusAsync(
            project.Id,
            ProjectStatus.InProgress,
            CancellationToken.None);

        Assert.Equal(ProjectStatus.InProgress, inProgress.Status);

        var completed = await projects.UpdateProgressAsync(
            project.Id,
            new UpdateProjectProgressRequest(100, "Entregado al cliente"),
            CancellationToken.None);

        Assert.Equal(ProjectStatus.Completed, completed.Status);
        Assert.Equal(100, completed.ProgressPercent);
        Assert.Equal(new DateOnly(2026, 11, 12), completed.ActualEndDate);

        var closedContract = await contracts.GetByIdAsync(contract.Id, CancellationToken.None);
        Assert.Equal(ContractStatus.Completed, closedContract.Status);
        Assert.Equal(project.Id, closedContract.ProjectId);

        await Assert.ThrowsAsync<ConflictException>(() =>
            projects.UpdateProgressAsync(
                project.Id,
                new UpdateProjectProgressRequest(50, null),
                CancellationToken.None));

        await Assert.ThrowsAsync<ConflictException>(() =>
            contracts.ChangeStatusAsync(
                contract.Id,
                ContractStatus.Cancelled,
                CancellationToken.None));

        Assert.Contains("requests.created", audit.Actions);
        Assert.Contains("quotes.sent", audit.Actions);
        Assert.Contains("quotes.accepted", audit.Actions);
        Assert.Contains("contracts.created", audit.Actions);
        Assert.Contains("projects.completed", audit.Actions);
        Assert.Contains("contracts.completed", audit.Actions);
    }

    [Fact]
    public async Task CreateQuote_SecondDraftIsConflict_AndNewVersionAfterReject()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);

        var request = await requests.CreateAsync(NewRequest(client), CancellationToken.None);
        var first = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None));

        await quotes.RejectAsync(
            first.Id,
            new RejectQuoteRequest("El cliente pidió ajustar el alcance"),
            CancellationToken.None);

        var second = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);

        Assert.Equal(2, second.Version);
        Assert.Equal("COT-00002", second.Code);
    }

    [Fact]
    public async Task AcceptQuote_RejectsSiblingQuotes()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);

        var request = await requests.CreateAsync(NewRequest(client), CancellationToken.None);

        var first = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);
        await quotes.SendAsync(first.Id, new SendQuoteRequest(null), CancellationToken.None);

        var second = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);
        await quotes.SendAsync(second.Id, new SendQuoteRequest(null), CancellationToken.None);

        await quotes.AcceptAsync(second.Id, CancellationToken.None);

        var rejected = await quotes.GetByIdAsync(first.Id, CancellationToken.None);

        Assert.Equal(QuoteStatus.Rejected, rejected.Status);
        Assert.NotNull(rejected.RejectedAt);
        Assert.Equal("Reemplazada por la cotización aceptada.", rejected.DecisionNotes);
    }

    [Fact]
    public async Task AcceptQuote_WhenExpired_MarksExpiredAndThrows()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);

        var request = await requests.CreateAsync(NewRequest(client), CancellationToken.None);
        var quote = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);
        await quotes.SendAsync(quote.Id, new SendQuoteRequest(null), CancellationToken.None);

        clock.UtcNow = new DateTimeOffset(2027, 1, 15, 12, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<ConflictException>(() =>
            quotes.AcceptAsync(quote.Id, CancellationToken.None));

        var expired = await quotes.GetByIdAsync(quote.Id, CancellationToken.None);
        Assert.Equal(QuoteStatus.Expired, expired.Status);
    }

    [Fact]
    public async Task UpdateQuote_WhenSent_ThrowsConflict()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);

        var request = await requests.CreateAsync(NewRequest(client), CancellationToken.None);
        var quote = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);
        await quotes.SendAsync(quote.Id, new SendQuoteRequest(null), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            quotes.UpdateAsync(
                quote.Id,
                new UpdateQuoteRequest(
                    "PEN",
                    0.18m,
                    null,
                    null,
                    [new QuoteItemRequest("Otro", null, 1, 10m)]),
                CancellationToken.None));
    }

    [Fact]
    public async Task Contract_RequiresAcceptedQuote_AndOnlyOnePerQuote()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);
        var contracts = new ContractService(db, audit, clock);

        var request = await requests.CreateAsync(NewRequest(client), CancellationToken.None);
        var quote = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            contracts.CreateAsync(
                new CreateContractRequest(quote.Id, null, null, null, null),
                CancellationToken.None));

        await quotes.SendAsync(quote.Id, new SendQuoteRequest(null), CancellationToken.None);
        await quotes.AcceptAsync(quote.Id, CancellationToken.None);

        await contracts.CreateAsync(
            new CreateContractRequest(quote.Id, null, null, null, null),
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            contracts.CreateAsync(
                new CreateContractRequest(quote.Id, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Project_RequiresActiveContract_AndOnlyOnePerContract()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);
        var contracts = new ContractService(db, audit, clock);
        var projects = new ProjectService(db, audit, clock);

        var request = await requests.CreateAsync(NewRequest(client), CancellationToken.None);
        var quote = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);
        await quotes.SendAsync(quote.Id, new SendQuoteRequest(null), CancellationToken.None);
        await quotes.AcceptAsync(quote.Id, CancellationToken.None);

        var contract = await contracts.CreateAsync(
            new CreateContractRequest(quote.Id, null, null, null, null),
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            projects.CreateAsync(
                new CreateProjectRequest(contract.Id, null, null, null, null, null, null),
                CancellationToken.None));

        await contracts.ChangeStatusAsync(contract.Id, ContractStatus.Active, CancellationToken.None);

        await projects.CreateAsync(
            new CreateProjectRequest(contract.Id, null, null, null, null, null, null),
            CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            projects.CreateAsync(
                new CreateProjectRequest(contract.Id, null, null, null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CancellingContract_CancelsAssociatedProject()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);
        var contracts = new ContractService(db, audit, clock);
        var projects = new ProjectService(db, audit, clock);

        var request = await requests.CreateAsync(NewRequest(client), CancellationToken.None);
        var quote = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);
        await quotes.SendAsync(quote.Id, new SendQuoteRequest(null), CancellationToken.None);
        await quotes.AcceptAsync(quote.Id, CancellationToken.None);

        var contract = await contracts.CreateAsync(
            new CreateContractRequest(quote.Id, null, null, null, null),
            CancellationToken.None);

        await contracts.ChangeStatusAsync(contract.Id, ContractStatus.Active, CancellationToken.None);

        var project = await projects.CreateAsync(
            new CreateProjectRequest(contract.Id, null, null, null, null, null, null),
            CancellationToken.None);

        await contracts.ChangeStatusAsync(contract.Id, ContractStatus.Cancelled, CancellationToken.None);

        var cancelledProject = await projects.GetByIdAsync(project.Id, CancellationToken.None);
        Assert.Equal(ProjectStatus.Cancelled, cancelledProject.Status);

        var reopenedRequest = await requests.GetByIdAsync(request.Id, CancellationToken.None);
        Assert.Equal(ProjectRequestStatus.InEvaluation, reopenedRequest.Status);

        var cancelledQuote = await quotes.GetByIdAsync(quote.Id, CancellationToken.None);
        Assert.Equal(QuoteStatus.Cancelled, cancelledQuote.Status);
        Assert.Contains("anulado", cancelledQuote.DecisionNotes);

        var newQuote = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);
        Assert.Equal(2, newQuote.Version);

        var cancelledRequest = await requests.ChangeStatusAsync(
            request.Id,
            ProjectRequestStatus.Cancelled,
            CancellationToken.None);

        Assert.Equal(ProjectRequestStatus.Cancelled, cancelledRequest.Status);

        var cancelledDraft = await quotes.GetByIdAsync(newQuote.Id, CancellationToken.None);
        Assert.Equal(QuoteStatus.Cancelled, cancelledDraft.Status);
    }

    [Fact]
    public void QuoteValidator_ProducesMemberPropertyNames()
    {
        var validator = new CreateQuoteRequestValidator();

        var valid = validator.Validate(new CreateQuoteRequest(
            Guid.NewGuid(),
            "PEN",
            0.18m,
            null,
            null,
            [new QuoteItemRequest("Equipo", "UND", 1, 100m)]));

        Assert.True(valid.IsValid);

        var invalid = validator.Validate(new CreateQuoteRequest(
            Guid.NewGuid(),
            "PEN",
            2m,
            null,
            null,
            [new QuoteItemRequest(string.Empty, "UND", 0, -1m)]));

        Assert.False(invalid.IsValid);
        Assert.Contains(invalid.Errors, error => error.PropertyName == "TaxRate");
        Assert.Contains(invalid.Errors, error => error.PropertyName == "Items[0].Description");
        Assert.Contains(invalid.Errors, error => error.PropertyName == "Items[0].Quantity");
        Assert.Contains(invalid.Errors, error => error.PropertyName == "Items[0].UnitPrice");
    }

    [Fact]
    public async Task CreateRequest_WithAddressFromAnotherClient_ThrowsNotFound()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);

        var otherClient = new Client { Code = "CLI-00002", Name = "Otro cliente", IsActive = true };
        otherClient.Addresses.Add(new ClientAddress
        {
            Label = "Otra planta",
            Line1 = "Av. Dos 456",
            IsPrimary = true,
        });

        db.Clients.Add(otherClient);
        await db.SaveChangesAsync();

        var foreignAddressId = db.ClientAddresses.Single(a => a.ClientId == otherClient.Id).Id;

        await Assert.ThrowsAsync<NotFoundException>(() =>
            requests.CreateAsync(NewRequest(client, foreignAddressId), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRequest_AfterSendingQuote_ThrowsConflict()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);

        var request = await requests.CreateAsync(NewRequest(client), CancellationToken.None);
        var quote = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);
        await quotes.SendAsync(quote.Id, new SendQuoteRequest(null), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            requests.UpdateAsync(
                request.Id,
                new UpdateProjectRequestRequest(
                    client.Id,
                    null,
                    "Alcance modificado",
                    null,
                    ProjectType.ColdRoom,
                    RequestPriority.Normal,
                    null,
                    null,
                    null),
                CancellationToken.None));
    }

    [Fact]
    public async Task CancellingRequest_CancelsOpenQuotesAndBlocksNewOnes()
    {
        var (db, client, _) = await SeedAsync();
        var clock = new FixedDateTimeProvider();
        var audit = new FakeAuditService();
        var requests = new ProjectRequestService(db, audit, clock);
        var quotes = new QuoteService(db, audit, clock);

        var request = await requests.CreateAsync(NewRequest(client), CancellationToken.None);
        var quote = await quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None);

        var cancelled = await requests.ChangeStatusAsync(
            request.Id,
            ProjectRequestStatus.Cancelled,
            CancellationToken.None);

        Assert.Equal(ProjectRequestStatus.Cancelled, cancelled.Status);

        var cancelledQuote = await quotes.GetByIdAsync(quote.Id, CancellationToken.None);
        Assert.Equal(QuoteStatus.Cancelled, cancelledQuote.Status);

        await Assert.ThrowsAsync<ConflictException>(() =>
            quotes.CreateAsync(NewQuote(request.Id), CancellationToken.None));
    }
}
