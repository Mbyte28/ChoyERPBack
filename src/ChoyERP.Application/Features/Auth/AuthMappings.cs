using ChoyERP.Application.Features.Auth.Dtos;
using ChoyERP.Domain.Entities.Users;

namespace ChoyERP.Application.Features.Auth;

internal static class AuthMappings
{
    public static AuthenticatedUserDto ToAuthenticatedUserDto(
        this User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions) =>
        new(
            user.Id,
            user.Email,
            user.Worker?.FullName,
            user.WorkerId,
            roles.ToArray(),
            permissions.ToArray());
}
