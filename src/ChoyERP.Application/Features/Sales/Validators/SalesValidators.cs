using ChoyERP.Application.Features.Sales.Dtos;
using FluentValidation;

namespace ChoyERP.Application.Features.Sales.Validators;

internal static class SalesRules
{
    public static void ApplyMoneyAndItems<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, string?>> currency,
        System.Linq.Expressions.Expression<Func<T, decimal>> taxRate,
        System.Linq.Expressions.Expression<Func<T, string?>> notes,
        System.Linq.Expressions.Expression<Func<T, IEnumerable<SaleItemRequest>?>> items)
    {
        validator.RuleFor(currency)
            .NotEmpty().WithMessage("La moneda es obligatoria.")
            .Length(3).WithMessage("La moneda debe tener tres caracteres, por ejemplo PEN.");

        validator.RuleFor(taxRate)
            .InclusiveBetween(0m, 1m)
            .WithMessage("La tasa de impuesto debe estar entre 0 y 1.");

        validator.RuleFor(notes).MaximumLength(2000);

        validator.RuleFor(items)
            .NotNull().WithMessage("La venta debe tener al menos una línea.")
            .NotEmpty().WithMessage("La venta debe tener al menos una línea.")
            .Must(lines => lines is null || lines.Count() <= 200)
            .WithMessage("La venta no puede tener más de 200 líneas.");

        validator.RuleForEach(items).SetValidator(new SaleItemRequestValidator());
    }
}

public sealed class SaleItemRequestValidator : AbstractValidator<SaleItemRequest>
{
    public SaleItemRequestValidator()
    {
        RuleFor(x => x.InventoryItemId).NotEmpty();

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");
    }
}

public sealed class CreateSaleRequestValidator : AbstractValidator<CreateSaleRequest>
{
    public CreateSaleRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();

        SalesRules.ApplyMoneyAndItems(
            this,
            x => x.Currency,
            x => x.TaxRate,
            x => x.Notes,
            x => x.Items);

        RuleFor(x => x.ExpectedDeliveryDate)
            .Must((request, expected) =>
                expected is null || request.SaleDate is null || expected >= request.SaleDate)
            .WithMessage("La fecha de entrega prevista no puede ser anterior a la fecha de venta.");
    }
}

public sealed class UpdateSaleRequestValidator : AbstractValidator<UpdateSaleRequest>
{
    public UpdateSaleRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();

        SalesRules.ApplyMoneyAndItems(
            this,
            x => x.Currency,
            x => x.TaxRate,
            x => x.Notes,
            x => x.Items);

        RuleFor(x => x.ExpectedDeliveryDate)
            .Must((request, expected) =>
                expected is null || request.SaleDate is null || expected >= request.SaleDate)
            .WithMessage("La fecha de entrega prevista no puede ser anterior a la fecha de venta.");
    }
}

public sealed class DeliverSaleRequestValidator : AbstractValidator<DeliverSaleRequest>
{
    public DeliverSaleRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class CancelSaleRequestValidator : AbstractValidator<CancelSaleRequest>
{
    public CancelSaleRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo de anulación es obligatorio.")
            .MaximumLength(500);
    }
}
