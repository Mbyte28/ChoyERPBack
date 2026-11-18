using ChoyERP.Application.Common.Models;
using ChoyERP.Domain.Entities.Contracting;

namespace ChoyERP.Application.Features.Contracting.Dtos;

public sealed record QuoteItemDto(
    Guid Id,
    string Description,
    string? Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record QuoteSummaryDto(
    Guid Id,
    string Code,
    int Version,
    QuoteStatus Status,
    decimal Total,
    string Currency);

public sealed record ProjectRequestDto(
    Guid Id,
    string Code,
    Guid ClientId,
    string ClientName,
    Guid? ClientAddressId,
    string? ClientAddressLabel,
    string Title,
    string? Description,
    ProjectType ProjectType,
    RequestPriority Priority,
    ProjectRequestStatus Status,
    DateOnly RequestedAt,
    Guid? ResponsibleWorkerId,
    string? ResponsibleWorkerName,
    string? Notes,
    IReadOnlyList<QuoteSummaryDto> Quotes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record QuoteDto(
    Guid Id,
    string Code,
    Guid ProjectRequestId,
    string ProjectRequestCode,
    string ProjectRequestTitle,
    Guid ClientId,
    string ClientName,
    int Version,
    QuoteStatus Status,
    string Currency,
    decimal TaxRate,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    DateOnly? ValidUntil,
    string? Notes,
    string? DecisionNotes,
    DateTimeOffset? SentAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RejectedAt,
    Guid? ContractId,
    string? ContractCode,
    IReadOnlyList<QuoteItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ContractDto(
    Guid Id,
    string Code,
    Guid QuoteId,
    string QuoteCode,
    Guid ProjectRequestId,
    string ProjectRequestCode,
    Guid ClientId,
    string ClientName,
    ContractStatus Status,
    decimal Amount,
    string Currency,
    DateOnly? SignedAt,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Notes,
    Guid? ProjectId,
    string? ProjectCode,
    ProjectStatus? ProjectStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProjectDto(
    Guid Id,
    string Code,
    Guid ContractId,
    string ContractCode,
    Guid QuoteId,
    string QuoteCode,
    Guid ClientId,
    string ClientName,
    Guid? ClientAddressId,
    string? Location,
    string Name,
    string? Description,
    ProjectStatus Status,
    int ProgressPercent,
    DateOnly? StartDate,
    DateOnly? ExpectedEndDate,
    DateOnly? ActualEndDate,
    Guid? ResponsibleWorkerId,
    string? ResponsibleWorkerName,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record QuoteItemRequest(
    string Description,
    string? Unit,
    decimal Quantity,
    decimal UnitPrice);

public sealed record CreateProjectRequestRequest(
    Guid ClientId,
    Guid? ClientAddressId,
    string Title,
    string? Description,
    ProjectType ProjectType,
    RequestPriority Priority,
    DateOnly? RequestedAt,
    Guid? ResponsibleWorkerId,
    string? Notes);

public sealed record UpdateProjectRequestRequest(
    Guid ClientId,
    Guid? ClientAddressId,
    string Title,
    string? Description,
    ProjectType ProjectType,
    RequestPriority Priority,
    DateOnly? RequestedAt,
    Guid? ResponsibleWorkerId,
    string? Notes);

public sealed record ChangeProjectRequestStatusRequest(ProjectRequestStatus Status);

public sealed record CreateQuoteRequest(
    Guid ProjectRequestId,
    string Currency,
    decimal TaxRate,
    DateOnly? ValidUntil,
    string? Notes,
    IReadOnlyList<QuoteItemRequest> Items);

public sealed record UpdateQuoteRequest(
    string Currency,
    decimal TaxRate,
    DateOnly? ValidUntil,
    string? Notes,
    IReadOnlyList<QuoteItemRequest> Items);

public sealed record SendQuoteRequest(DateOnly? ValidUntil);

public sealed record RejectQuoteRequest(string? Reason);

public sealed record CreateContractRequest(
    Guid QuoteId,
    DateOnly? SignedAt,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Notes);

public sealed record UpdateContractRequest(
    DateOnly? SignedAt,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Notes);

public sealed record ChangeContractStatusRequest(ContractStatus Status);

public sealed record CreateProjectRequest(
    Guid ContractId,
    string? Name,
    string? Description,
    DateOnly? StartDate,
    DateOnly? ExpectedEndDate,
    Guid? ResponsibleWorkerId,
    string? Notes);

public sealed record UpdateProjectRequest(
    string Name,
    string? Description,
    DateOnly? StartDate,
    DateOnly? ExpectedEndDate,
    Guid? ResponsibleWorkerId,
    string? Location,
    string? Notes);

public sealed record UpdateProjectProgressRequest(int ProgressPercent, string? Notes);

public sealed record ChangeProjectStatusRequest(ProjectStatus Status);

public sealed class ProjectRequestQuery : PaginationQuery
{
    public string? Search { get; set; }

    public ProjectRequestStatus? Status { get; set; }

    public Guid? ClientId { get; set; }

    public ProjectType? ProjectType { get; set; }

    public RequestPriority? Priority { get; set; }

    public Guid? ResponsibleWorkerId { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}

public sealed class QuoteQuery : PaginationQuery
{
    public string? Search { get; set; }

    public QuoteStatus? Status { get; set; }

    public Guid? ProjectRequestId { get; set; }

    public Guid? ClientId { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}

public sealed class ContractQuery : PaginationQuery
{
    public string? Search { get; set; }

    public ContractStatus? Status { get; set; }

    public Guid? ClientId { get; set; }

    public Guid? QuoteId { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}

public sealed class ProjectQuery : PaginationQuery
{
    public string? Search { get; set; }

    public ProjectStatus? Status { get; set; }

    public Guid? ClientId { get; set; }

    public Guid? ContractId { get; set; }

    public Guid? ResponsibleWorkerId { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}
