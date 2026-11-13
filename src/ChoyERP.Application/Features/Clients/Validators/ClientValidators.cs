using ChoyERP.Application.Features.Clients.Dtos;
using FluentValidation;

namespace ChoyERP.Application.Features.Clients.Validators;

internal static class ClientRules
{
    public static void ApplyCommon<T>(
        AbstractValidator<T> validator,
        Func<T, string?> name,
        Func<T, string?> code,
        Func<T, string?> legalName,
        Func<T, string?> taxId,
        Func<T, string?> contactName,
        Func<T, string?> email,
        Func<T, string?> phone,
        Func<T, string?> notes)
    {
        validator.RuleFor(x => name(x))
            .NotEmpty().WithMessage("El nombre del cliente es obligatorio.")
            .MaximumLength(200);

        validator.RuleFor(x => code(x)).MaximumLength(30);
        validator.RuleFor(x => legalName(x)).MaximumLength(200);
        validator.RuleFor(x => taxId(x)).MaximumLength(30);
        validator.RuleFor(x => contactName(x)).MaximumLength(150);
        validator.RuleFor(x => phone(x)).MaximumLength(30);
        validator.RuleFor(x => notes(x)).MaximumLength(2000);

        validator.RuleFor(x => email(x))
            .EmailAddress().WithMessage("El correo no tiene un formato válido.")
            .MaximumLength(320)
            .When(x => !string.IsNullOrWhiteSpace(email(x)));
    }
}

public sealed class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        ClientRules.ApplyCommon(
            this,
            x => x.Name,
            x => x.Code,
            x => x.LegalName,
            x => x.TaxId,
            x => x.ContactName,
            x => x.Email,
            x => x.Phone,
            x => x.Notes);
    }
}

public sealed class UpdateClientRequestValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientRequestValidator()
    {
        ClientRules.ApplyCommon(
            this,
            x => x.Name,
            x => x.Code,
            x => x.LegalName,
            x => x.TaxId,
            x => x.ContactName,
            x => x.Email,
            x => x.Phone,
            x => x.Notes);
    }
}

internal static class ClientAddressRules
{
    public static void Apply<T>(
        AbstractValidator<T> validator,
        Func<T, string?> label,
        Func<T, string?> line1,
        Func<T, string?> line2,
        Func<T, string?> city,
        Func<T, string?> state,
        Func<T, string?> country,
        Func<T, string?> postalCode)
    {
        validator.RuleFor(x => label(x))
            .NotEmpty().WithMessage("La etiqueta de la dirección es obligatoria.")
            .MaximumLength(100);

        validator.RuleFor(x => line1(x))
            .NotEmpty().WithMessage("La dirección es obligatoria.")
            .MaximumLength(250);

        validator.RuleFor(x => line2(x)).MaximumLength(250);
        validator.RuleFor(x => city(x)).MaximumLength(100);
        validator.RuleFor(x => state(x)).MaximumLength(100);
        validator.RuleFor(x => country(x)).MaximumLength(100);
        validator.RuleFor(x => postalCode(x)).MaximumLength(20);
    }
}

public sealed class CreateClientAddressRequestValidator : AbstractValidator<CreateClientAddressRequest>
{
    public CreateClientAddressRequestValidator()
    {
        ClientAddressRules.Apply(
            this,
            x => x.Label,
            x => x.Line1,
            x => x.Line2,
            x => x.City,
            x => x.State,
            x => x.Country,
            x => x.PostalCode);
    }
}

public sealed class UpdateClientAddressRequestValidator : AbstractValidator<UpdateClientAddressRequest>
{
    public UpdateClientAddressRequestValidator()
    {
        ClientAddressRules.Apply(
            this,
            x => x.Label,
            x => x.Line1,
            x => x.Line2,
            x => x.City,
            x => x.State,
            x => x.Country,
            x => x.PostalCode);
    }
}
