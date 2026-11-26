using FluentValidation;
using Invelop.Contacts.Domain;

namespace Invelop.Contacts.Application.Contacts.Create;

/// <summary>
/// Validates the incoming <b>model</b> at the API boundary.
/// </summary>
/// <remarks>
/// This is the validator a user's mistakes actually meet. Its job is a good error message against
/// the right field, and all of them at once rather than one per round trip.
/// <para>
/// Note what it does <em>not</em> do: it does not implement a single rule. Every
/// <c>Must(...)</c> below hands off to a predicate that lives in the domain beside the type it
/// describes. That is the whole anti-drift mechanism - there is no second copy of "what makes an
/// IBAN valid" to fall out of step with the first. The <c>Cascade.Stop</c> is so a blank field
/// reports "required" rather than also reporting that it is malformed.
/// </para>
/// </remarks>
public sealed class CreateContactCommandValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.FirstName)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ContactValidationMessages.FirstNameRequired)
            .Must(PersonName.IsValidPart).WithMessage(ContactValidationMessages.FirstNameInvalid);

        RuleFor(command => command.Surname)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ContactValidationMessages.SurnameRequired)
            .Must(PersonName.IsValidPart).WithMessage(ContactValidationMessages.SurnameInvalid);

        RuleFor(command => command.DateOfBirth)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage(ContactValidationMessages.DateOfBirthRequired)
            .Must(dateOfBirth => DateOfBirthRules.IsValid(dateOfBirth!.Value, DateOfBirthRules.Today(timeProvider)))
            .WithMessage(ContactValidationMessages.DateOfBirthInvalid);

        RuleFor(command => command.Address)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ContactValidationMessages.AddressRequired)
            .Must(Address.IsWellFormed).WithMessage(ContactValidationMessages.AddressInvalid);

        RuleFor(command => command.PhoneNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ContactValidationMessages.PhoneNumberRequired)
            .Must(PhoneNumber.IsWellFormed).WithMessage(ContactValidationMessages.PhoneNumberInvalid);

        RuleFor(command => command.Iban)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ContactValidationMessages.IbanRequired)
            .Must(Iban.IsWellFormed).WithMessage(ContactValidationMessages.IbanInvalid);
    }
}
