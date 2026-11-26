using Invelop.Contacts.Application.Abstractions;
using Invelop.Contacts.Domain;

namespace Invelop.Contacts.Application.Contacts.Create;

/// <summary>
/// The write side: turns a validated command into a persisted aggregate.
/// </summary>
/// <remarks>
/// A plain class with one public method, injected straight into the endpoint. There is no
/// mediator because there is nothing for one to do here - no pipeline to assemble, no dynamic
/// dispatch to resolve. Registering this with an <c>ISender</c> would add a layer of indirection
/// between the endpoint and the five lines below without changing any of them.
/// <para>
/// Note the shape against <see cref="GetContactsHandler"/>: this side builds the full domain
/// object and goes through the factories. The read side never materialises one. That asymmetry
/// is what CQRS is actually for.
/// </para>
/// </remarks>
public sealed class CreateContactHandler(IAppDbContext database, TimeProvider timeProvider)
{
    public async Task<ContactDto> Handle(CreateContactCommand command, CancellationToken cancellationToken)
    {
        // Each Create below re-validates and normalises. By the time Contact.Create runs, every
        // value object is known-good, so the aggregate only has to police the one rule that is
        // its own: the date of birth.
        var contact = Contact.Create(
            PersonName.Create(command.FirstName, command.Surname),
            command.DateOfBirth!.Value,
            Address.Create(command.Address),
            PhoneNumber.Create(command.PhoneNumber),
            Iban.Create(command.Iban),
            timeProvider);

        database.Contacts.Add(contact);
        await database.SaveChangesAsync(cancellationToken);

        return contact.ToDto();
    }
}
