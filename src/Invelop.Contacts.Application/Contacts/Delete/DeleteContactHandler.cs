using Invelop.Contacts.Application.Abstractions;

namespace Invelop.Contacts.Application.Contacts.Delete;

public sealed class DeleteContactHandler(IAppDbContext database)
{
    public async Task<bool> Handle(Guid id, CancellationToken cancellationToken)
    {
        var contact = await database.Contacts.FindAsync([id], cancellationToken);
        if (contact is null)
        {
            return false;
        }

        database.Contacts.Remove(contact);
        await database.SaveChangesAsync(cancellationToken);
        return true;
    }
}
