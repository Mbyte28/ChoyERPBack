using ChoyERP.Application.Common.Models;

namespace ChoyERP.Application.Features.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    bool IsActive,
    Guid? WorkerId,
    string? WorkerFullName,
    IReadOnlyList<RoleSummaryDto> Roles,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record RoleSummaryDto(Guid Id, string Name);

public sealed record CreateUserRequest(
    string Email,
    string Password,
    Guid? WorkerId,
    IReadOnlyList<Guid> RoleIds,
    bool IsActive = true);

public sealed record UpdateUserRequest(
    string Email,
    Guid? WorkerId,
    IReadOnlyList<Guid> RoleIds,
    bool IsActive);

public sealed record ResetPasswordRequest(string NewPassword);

public sealed class UserQuery : PaginationQuery
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public Guid? RoleId { get; set; }

    public string? SortBy { get; set; }

    public bool SortDesc { get; set; }
}
