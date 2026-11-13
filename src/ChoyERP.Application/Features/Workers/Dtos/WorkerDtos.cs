using ChoyERP.Application.Common.Models;

namespace ChoyERP.Application.Features.Workers.Dtos;

public sealed record WorkerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string? DocumentType,
    string DocumentNumber,
    string? Position,
    string? Department,
    string? Phone,
    string? Email,
    DateOnly? HireDate,
    bool IsActive,
    bool HasUserAccount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateWorkerRequest(
    string FirstName,
    string LastName,
    string? DocumentType,
    string DocumentNumber,
    string? Position,
    string? Department,
    string? Phone,
    string? Email,
    DateOnly? HireDate,
    string? Notes);

public sealed record UpdateWorkerRequest(
    string FirstName,
    string LastName,
    string? DocumentType,
    string DocumentNumber,
    string? Position,
    string? Department,
    string? Phone,
    string? Email,
    DateOnly? HireDate,
    string? Notes);

public sealed class WorkerQuery : PaginationQuery
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}
