namespace ChoyERP.Application.Features.Auth.Dtos;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string? RefreshToken);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record AuthenticatedUserDto(
    Guid Id,
    string Email,
    string? FullName,
    Guid? WorkerId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthenticatedUserDto User);
