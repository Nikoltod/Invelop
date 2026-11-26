using Invelop.Contacts.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Invelop.Contacts.Application.Contacts.GetAll;

/// <summary>
/// The read side: projects contacts straight into DTOs.
/// </summary>
/// <remarks>
/// Compare this with <c>CreateContactHandler</c>. No aggregate is ever constructed here - no
/// value objects, no factories, no change tracking. The projection is translated into the SELECT
/// list, so SQLite returns exactly the six columns the client renders and nothing else.
/// <para>
/// There is no <c>GetContactsQuery</c> parameter object. An empty record would be pure ceremony
/// today; when this grows a search term or paging it earns one, and adding it then is a
/// two-minute change. CQRS is the separation of the read and write models, which is already
/// here - not the presence of a message class.
/// </para>
/// </remarks>
public sealed class GetContactsHandler(IAppDbContext database)
{
    public async Task<IReadOnlyList<ContactDto>> Handle(CancellationToken cancellationToken)
    {
        var contacts = await database.Contacts
            .AsNoTracking()
            .Select(contact => new ContactDto(
                contact.Id,
                contact.Name.First,
                contact.Name.Surname,
                contact.DateOfBirth,
                contact.Address.Value,
                contact.PhoneNumber.Value,
                contact.Iban.Value))
            .ToListAsync(cancellationToken);

        return contacts
            .OrderBy(contact => contact.Surname, StringComparer.OrdinalIgnoreCase)
            .ThenBy(contact => contact.FirstName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
