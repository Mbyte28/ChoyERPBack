using ChoyERP.Application.Common.Security;
using ChoyERP.Application.Features.Users.Dtos;
using FluentValidation;

namespace ChoyERP.Application.Features.Users.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(320).WithMessage("El correo no puede superar los 320 caracteres.");

        RuleFor(x => x.Password)
            .Must(PasswordPolicy.IsValid).WithMessage(PasswordPolicy.Description);

        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("Debe asignar al menos un rol.");
    }
}

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(320).WithMessage("El correo no puede superar los 320 caracteres.");

        RuleFor(x => x.RoleIds)
            .NotEmpty().WithMessage("Debe asignar al menos un rol.");
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .Must(PasswordPolicy.IsValid).WithMessage(PasswordPolicy.Description);
    }
}
