using ChoyERP.Application.Common.Models;
using ChoyERP.Domain.Entities.Users;

namespace ChoyERP.Application.Common.Interfaces;

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions);
}
