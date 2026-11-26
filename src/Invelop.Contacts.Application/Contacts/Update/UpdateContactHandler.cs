using Invelop.Contacts.Application.Abstractions;
using Invelop.Contacts.Domain;

namespace Invelop.Contacts.Application.Contacts.Update;

public sealed class UpdateContactHandler(IAppDbContext database, TimeProvider timeProvider)
{
    public async Task<ContactDto?> Handle(Guid id, UpdateContactCommand command, CancellationToken cancellationToken)
    {
        var contact = await database.Contacts.FindAsync([id], cancellationToken);
        if (contact is null)
        {
            return null;
        }

        contact.Rename(PersonName.Create(command.FirstName, command.Surname));
        contact.CorrectDateOfBirth(command.DateOfBirth!.Value, timeProvider);
        contact.MoveTo(Address.Create(command.Address));
        contact.ChangePhoneNumber(PhoneNumber.Create(command.PhoneNumber));
        contact.ChangeIban(Iban.Create(command.Iban));

        await database.SaveChangesAsync(cancellationToken);
        return contact.ToDto();
    }
}
