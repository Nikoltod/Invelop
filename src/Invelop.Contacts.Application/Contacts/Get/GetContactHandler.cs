using Invelop.Contacts.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Invelop.Contacts.Application.Contacts.Get;

public sealed class GetContactHandler(IAppDbContext database)
{
    public async Task<ContactDto?> Handle(Guid id, CancellationToken cancellationToken) =>
        await database.Contacts
            .AsNoTracking()
            .Where(contact => contact.Id == id)
            .Select(contact => new ContactDto(
                contact.Id,
                contact.Name.First,
                contact.Name.Surname,
                contact.DateOfBirth,
                contact.Address.Value,
                contact.PhoneNumber.Value,
                contact.Iban.Value))
            .SingleOrDefaultAsync(cancellationToken);
}
