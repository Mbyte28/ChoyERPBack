using ChoyERP.Application.Features.Workers.Dtos;
using FluentValidation;

namespace ChoyERP.Application.Features.Workers.Validators;

internal static class WorkerRules
{
    public static void Apply(AbstractValidator<CreateWorkerRequest> validator)
    {
        validator.RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MaximumLength(100);

        validator.RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100);

        validator.RuleFor(x => x.DocumentType).MaximumLength(20);
        validator.RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("El número de documento es obligatorio.")
            .MaximumLength(30);
        validator.RuleFor(x => x.Position).MaximumLength(100);
        validator.RuleFor(x => x.Department).MaximumLength(100);
        validator.RuleFor(x => x.Phone).MaximumLength(30);
        validator.RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(320)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        validator.RuleFor(x => x.Notes).MaximumLength(2000);
        validator.RuleFor(x => x.HireDate)
            .Must(date => date is null || date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha de ingreso no puede ser futura.");
    }

    public static void Apply(AbstractValidator<UpdateWorkerRequest> validator)
    {
        validator.RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Los nombres son obligatorios.")
            .MaximumLength(100);

        validator.RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Los apellidos son obligatorios.")
            .MaximumLength(100);

        validator.RuleFor(x => x.DocumentType).MaximumLength(20);
        validator.RuleFor(x => x.DocumentNumber)
            .NotEmpty().WithMessage("El número de documento es obligatorio.")
            .MaximumLength(30);
        validator.RuleFor(x => x.Position).MaximumLength(100);
        validator.RuleFor(x => x.Department).MaximumLength(100);
        validator.RuleFor(x => x.Phone).MaximumLength(30);
        validator.RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(320)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        validator.RuleFor(x => x.Notes).MaximumLength(2000);
        validator.RuleFor(x => x.HireDate)
            .Must(date => date is null || date <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("La fecha de ingreso no puede ser futura.");
    }
}

public sealed class CreateWorkerRequestValidator : AbstractValidator<CreateWorkerRequest>
{
    public CreateWorkerRequestValidator() => WorkerRules.Apply(this);
}

public sealed class UpdateWorkerRequestValidator : AbstractValidator<UpdateWorkerRequest>
{
    public UpdateWorkerRequestValidator() => WorkerRules.Apply(this);
}
