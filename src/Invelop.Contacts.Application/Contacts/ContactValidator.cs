using FluentValidation;
using Invelop.Contacts.Domain;

namespace Invelop.Contacts.Application.Contacts;

/// <summary>
/// Validates the <see cref="Contact"/> <b>entity</b> itself, as opposed to the command that asked
/// for it. Run from <c>AppDbContext.SaveChangesAsync</c> over every added or modified contact.
/// </summary>
/// <remarks>
/// In normal operation this never fails. Input is rejected at the API boundary, and the domain
/// factories make an invalid <see cref="Contact"/> unconstructible. It exists for the paths that
/// genuinely bypass both:
/// <list type="number">
/// <item>
/// <b>EF materialisation.</b> Loading a row does not call <c>Contact.Create</c>; EF binds the
/// constructor and writes the columns. A row corrupted outside the application - hand-run SQL, a
/// restored backup, a bad data migration - therefore rehydrates into a perfectly ordinary-looking
/// object. Edit an unrelated field and save, and the invalid value is written straight back.
/// This refuses that write. The factory structurally cannot, because it never ran.
/// </item>
/// <item>
/// <b>Reflection-based mutation.</b> Object mappers, JSON Patch, seeders and test fixtures can
/// set a tracked property without any behaviour method being involved.
/// </item>
/// <item>
/// <b>Code paths that do not exist yet.</b> A bulk importer written next year calls neither the
/// command validator nor, necessarily, the factory. SaveChanges is the one chokepoint every
/// write has to pass through.
/// </item>
/// <item>
/// <b>Drift.</b> If the boundary and the domain ever disagree about a rule, this fails loudly at
/// persist time instead of quietly storing bad data.
/// </item>
/// </list>
/// Note that it validates against the same shared predicates as the boundary, so a disagreement
/// between the two is a code change, not an accident.
/// </remarks>
public sealed class ContactValidator : AbstractValidator<Contact>
{
    public ContactValidator(TimeProvider timeProvider)
    {
        RuleFor(contact => contact.Name.First)
            .Must(PersonName.IsValidPart)
            .WithMessage(ContactValidationMessages.FirstNameInvalid)
            .OverridePropertyName("FirstName");

        RuleFor(contact => contact.Name.Surname)
            .Must(PersonName.IsValidPart)
            .WithMessage(ContactValidationMessages.SurnameInvalid)
            .OverridePropertyName("Surname");

        RuleFor(contact => contact.DateOfBirth)
            .Must(dateOfBirth => DateOfBirthRules.IsValid(dateOfBirth, DateOfBirthRules.Today(timeProvider)))
            .WithMessage(ContactValidationMessages.DateOfBirthInvalid);

        RuleFor(contact => contact.Address.Value)
            .Must(Address.IsWellFormed)
            .WithMessage(ContactValidationMessages.AddressInvalid)
            .OverridePropertyName("Address");

        RuleFor(contact => contact.PhoneNumber.Value)
            .Must(PhoneNumber.IsWellFormed)
            .WithMessage(ContactValidationMessages.PhoneNumberInvalid)
            .OverridePropertyName("PhoneNumber");

        RuleFor(contact => contact.Iban.Value)
            .Must(Iban.IsWellFormed)
            .WithMessage(ContactValidationMessages.IbanInvalid)
            .OverridePropertyName("Iban");
    }
}
