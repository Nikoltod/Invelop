using FluentValidation;
using Invelop.Contacts.Domain;

namespace Invelop.Contacts.Application.Contacts.Update;

public sealed class UpdateContactCommandValidator : AbstractValidator<UpdateContactCommand>
{
    public UpdateContactCommandValidator(TimeProvider timeProvider)
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
