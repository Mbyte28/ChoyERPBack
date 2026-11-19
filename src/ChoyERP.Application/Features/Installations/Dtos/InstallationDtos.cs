using ChoyERP.Application.Common.Models;
using ChoyERP.Domain.Entities.Installations;

namespace ChoyERP.Application.Features.Installations.Dtos;

public sealed record InstallationDto(
    Guid Id,
    string Code,
    Guid ClientId,
    string ClientName,
    Guid? SaleId,
    string? SaleCode,
    Guid? ProjectId,
    string? ProjectCode,
    Guid? ClientAddressId,
    string? Address,
    string? Location,
    string Name,
    string? Description,
    InstallationStatus Status,
    DateOnly? ScheduledDate,
    DateOnly? StartedDate,
    DateOnly? CompletedDate,
    Guid? TechnicianWorkerId,
    string? TechnicianName,
    string? CancellationReason,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateInstallationRequest(
    Guid? SaleId,
    Guid? ProjectId,
    Guid? ClientAddressId,
    string? Location,
    string? Name,
    string? Description,
    DateOnly? ScheduledDate,
    Guid? TechnicianWorkerId,
    string? Notes);

public sealed record UpdateInstallationRequest(
    Guid? ClientAddressId,
    string? Location,
    string Name,
    string? Description,
    DateOnly? ScheduledDate,
    Guid? TechnicianWorkerId,
    string? Notes);

public sealed record StartInstallationRequest(DateOnly? StartedDate);

public sealed record CompleteInstallationRequest(DateOnly? CompletedDate, string? Notes);

public sealed record CancelInstallationRequest(string Reason);

public sealed class InstallationQuery : PaginationQuery
{
    public string? Search { get; set; }

    public InstallationStatus? Status { get; set; }

    public Guid? ClientId { get; set; }

    public Guid? SaleId { get; set; }

    public Guid? ProjectId { get; set; }

    public Guid? TechnicianWorkerId { get; set; }

    public DateOnly? FromDate { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}
