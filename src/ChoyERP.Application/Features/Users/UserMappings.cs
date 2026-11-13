using ChoyERP.Application.Features.Users.Dtos;
using ChoyERP.Domain.Entities.Users;

namespace ChoyERP.Application.Features.Users;

internal static class UserMappings
{
    public static UserDto ToDto(this User user) =>
        new(
            user.Id,
            user.Email,
            user.IsActive,
            user.WorkerId,
            user.Worker?.FullName,
            user.UserRoles
                .Select(ur => new RoleSummaryDto(ur.Role.Id, ur.Role.Name))
                .OrderBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            user.LastLoginAt,
            user.CreatedAt,
            user.UpdatedAt);
}
