using ChoyERP.Application.Features.Equipment.Dtos;
using FluentValidation;

namespace ChoyERP.Application.Features.Equipment.Validators;

public sealed class CreateEquipmentRequestValidator : AbstractValidator<CreateEquipmentRequest>
{
    public CreateEquipmentRequestValidator()
    {
        RuleFor(x => x.InstallationId).NotEmpty();
        RuleFor(x => x.InventoryItemId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.Brand).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class UpdateEquipmentRequestValidator : AbstractValidator<UpdateEquipmentRequest>
{
    public UpdateEquipmentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del equipo es obligatorio.")
            .MaximumLength(200);

        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.Brand).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class ChangeEquipmentStatusRequestValidator
    : AbstractValidator<ChangeEquipmentStatusRequest>
{
    public ChangeEquipmentStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo del cambio de estado es obligatorio.")
            .MaximumLength(500);
    }
}
