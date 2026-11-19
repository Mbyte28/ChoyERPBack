using ChoyERP.Application.Features.Installations.Dtos;
using FluentValidation;

namespace ChoyERP.Application.Features.Installations.Validators;

public sealed class CreateInstallationRequestValidator
    : AbstractValidator<CreateInstallationRequest>
{
    public CreateInstallationRequestValidator()
    {
        RuleFor(x => x.SaleId)
            .Must((request, saleId) => (saleId is not null) != (request.ProjectId is not null))
            .WithMessage("La instalación debe asociarse a una venta o a un proyecto, no a ambos.");

        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class UpdateInstallationRequestValidator
    : AbstractValidator<UpdateInstallationRequest>
{
    public UpdateInstallationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre de la instalación es obligatorio.")
            .MaximumLength(200);

        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class StartInstallationRequestValidator
    : AbstractValidator<StartInstallationRequest>
{
}

public sealed class CompleteInstallationRequestValidator
    : AbstractValidator<CompleteInstallationRequest>
{
    public CompleteInstallationRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class CancelInstallationRequestValidator
    : AbstractValidator<CancelInstallationRequest>
{
    public CancelInstallationRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo de anulación es obligatorio.")
            .MaximumLength(500);
    }
}
