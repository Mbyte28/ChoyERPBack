using System.Linq.Expressions;
using ChoyERP.Application.Features.Contracting.Dtos;
using FluentValidation;

namespace ChoyERP.Application.Features.Contracting.Validators;

internal static class ContractingRules
{
    public static void ApplyRequestCommon<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, Guid>> clientId,
        Expression<Func<T, string?>> title,
        Expression<Func<T, string?>> description,
        Expression<Func<T, string?>> notes)
    {
        validator.RuleFor(clientId).NotEmpty();

        validator.RuleFor(title)
            .NotEmpty().WithMessage("El título de la solicitud es obligatorio.")
            .MaximumLength(200);

        validator.RuleFor(description).MaximumLength(2000);
        validator.RuleFor(notes).MaximumLength(2000);
    }

    public static void ApplyQuoteItems<T>(
        AbstractValidator<T> validator,
        Expression<Func<T, IEnumerable<QuoteItemRequest>?>> items,
        Expression<Func<T, decimal>> taxRate,
        Expression<Func<T, string?>> currency,
        Expression<Func<T, string?>> notes)
    {
        validator.RuleFor(items)
            .NotNull().WithMessage("La cotización debe tener al menos una línea.")
            .NotEmpty().WithMessage("La cotización debe tener al menos una línea.")
            .Must(lines => lines is null || lines.Count() <= 200)
            .WithMessage("La cotización no puede tener más de 200 líneas.");

        validator.RuleForEach(items).SetValidator(new QuoteItemRequestValidator());

        validator.RuleFor(taxRate)
            .InclusiveBetween(0m, 1m)
            .WithMessage("La tasa de impuesto debe estar entre 0 y 1.");

        validator.RuleFor(currency)
            .NotEmpty().WithMessage("La moneda es obligatoria.")
            .Length(3).WithMessage("La moneda debe tener tres caracteres, por ejemplo PEN.");

        validator.RuleFor(notes).MaximumLength(2000);
    }
}

public sealed class QuoteItemRequestValidator : AbstractValidator<QuoteItemRequest>
{
    public QuoteItemRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción de la línea es obligatoria.")
            .MaximumLength(500);

        RuleFor(x => x.Unit).MaximumLength(20);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor que cero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo.");
    }
}

public sealed class CreateProjectRequestRequestValidator : AbstractValidator<CreateProjectRequestRequest>
{
    public CreateProjectRequestRequestValidator()
    {
        ContractingRules.ApplyRequestCommon(
            this,
            x => x.ClientId,
            x => x.Title,
            x => x.Description,
            x => x.Notes);

        RuleFor(x => x.ProjectType).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public sealed class UpdateProjectRequestRequestValidator : AbstractValidator<UpdateProjectRequestRequest>
{
    public UpdateProjectRequestRequestValidator()
    {
        ContractingRules.ApplyRequestCommon(
            this,
            x => x.ClientId,
            x => x.Title,
            x => x.Description,
            x => x.Notes);

        RuleFor(x => x.ProjectType).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public sealed class ChangeProjectRequestStatusRequestValidator
    : AbstractValidator<ChangeProjectRequestStatusRequest>
{
    public ChangeProjectRequestStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class CreateQuoteRequestValidator : AbstractValidator<CreateQuoteRequest>
{
    public CreateQuoteRequestValidator()
    {
        RuleFor(x => x.ProjectRequestId).NotEmpty();

        ContractingRules.ApplyQuoteItems(
            this,
            x => x.Items,
            x => x.TaxRate,
            x => x.Currency,
            x => x.Notes);
    }
}

public sealed class UpdateQuoteRequestValidator : AbstractValidator<UpdateQuoteRequest>
{
    public UpdateQuoteRequestValidator()
    {
        ContractingRules.ApplyQuoteItems(
            this,
            x => x.Items,
            x => x.TaxRate,
            x => x.Currency,
            x => x.Notes);
    }
}

public sealed class RejectQuoteRequestValidator : AbstractValidator<RejectQuoteRequest>
{
    public RejectQuoteRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(1000);
    }
}

public sealed class CreateContractRequestValidator : AbstractValidator<CreateContractRequest>
{
    public CreateContractRequestValidator()
    {
        RuleFor(x => x.QuoteId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.EndDate)
            .Must((request, endDate) =>
                endDate is null || request.StartDate is null || endDate >= request.StartDate)
            .WithMessage("La fecha de fin no puede ser anterior a la fecha de inicio.");
    }
}

public sealed class UpdateContractRequestValidator : AbstractValidator<UpdateContractRequest>
{
    public UpdateContractRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.EndDate)
            .Must((request, endDate) =>
                endDate is null || request.StartDate is null || endDate >= request.StartDate)
            .WithMessage("La fecha de fin no puede ser anterior a la fecha de inicio.");
    }
}

public sealed class ChangeContractStatusRequestValidator : AbstractValidator<ChangeContractStatusRequest>
{
    public ChangeContractStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.ExpectedEndDate)
            .Must((request, endDate) =>
                endDate is null || request.StartDate is null || endDate >= request.StartDate)
            .WithMessage("La fecha de fin no puede ser anterior a la fecha de inicio.");
    }
}

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del proyecto es obligatorio.")
            .MaximumLength(200);

        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Location).MaximumLength(300);
        RuleFor(x => x.Notes).MaximumLength(2000);

        RuleFor(x => x.ExpectedEndDate)
            .Must((request, endDate) =>
                endDate is null || request.StartDate is null || endDate >= request.StartDate)
            .WithMessage("La fecha de fin no puede ser anterior a la fecha de inicio.");
    }
}

public sealed class UpdateProjectProgressRequestValidator : AbstractValidator<UpdateProjectProgressRequest>
{
    public UpdateProjectProgressRequestValidator()
    {
        RuleFor(x => x.ProgressPercent)
            .InclusiveBetween(0, 100)
            .WithMessage("El avance debe estar entre 0 y 100.");

        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class ChangeProjectStatusRequestValidator : AbstractValidator<ChangeProjectStatusRequest>
{
    public ChangeProjectStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}
