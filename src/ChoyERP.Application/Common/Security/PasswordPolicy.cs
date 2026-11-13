namespace ChoyERP.Application.Common.Security;

public static class PasswordPolicy
{
    public const int MinimumLength = 10;

    public static bool IsValid(string? password) =>
        !string.IsNullOrWhiteSpace(password)
        && password.Length >= MinimumLength
        && password.Any(char.IsLetter)
        && password.Any(char.IsDigit);

    public const string Description =
        "La contraseña debe tener al menos 10 caracteres e incluir letras y números.";
}
