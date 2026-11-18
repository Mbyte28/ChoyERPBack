using ChoyERP.Application.Features.Inventory.Dtos;
using ChoyERP.Domain.Entities.Inventory;
using FluentValidation;

namespace ChoyERP.Application.Features.Inventory.Validators;

public sealed class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Code).MaximumLength(30);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del artículo es obligatorio.")
            .MaximumLength(200);

        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.Brand).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo.");

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo unitario no puede ser negativo.");
    }
}

public sealed class UpdateInventoryItemRequestValidator : AbstractValidator<UpdateInventoryItemRequest>
{
    public UpdateInventoryItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del artículo es obligatorio.")
            .MaximumLength(200);

        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.Brand).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.Location).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock mínimo no puede ser negativo.");
    }
}

public sealed class CreateInventoryMovementRequestValidator
    : AbstractValidator<CreateInventoryMovementRequest>
{
    public CreateInventoryMovementRequestValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();

        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("La cantidad no puede ser negativa.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.")
            .When(x => x.Type != InventoryMovementType.Adjustment);

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo unitario no puede ser negativo.")
            .When(x => x.UnitCost.HasValue);

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo del ajuste es obligatorio.")
            .When(x => x.Type == InventoryMovementType.Adjustment);

        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
