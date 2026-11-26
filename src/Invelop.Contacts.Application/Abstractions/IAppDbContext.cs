using Invelop.Contacts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Invelop.Contacts.Application.Abstractions;

/// <summary>
/// The persistence surface the handlers are allowed to see.
/// </summary>
/// <remarks>
/// Handlers depend on this rather than on <c>AppDbContext</c> so that Application never
/// references a database provider, and so tests can supply a context backed by in-memory SQLite.
/// <para>
/// This is deliberately not a repository per aggregate. <see cref="DbSet{TEntity}"/> already is
/// a repository and a unit of work; wrapping it in a hand-written <c>IContactRepository</c> that
/// forwards five methods would add a layer without adding a seam.
/// </para>
/// </remarks>
public interface IAppDbContext
{
    DbSet<Contact> Contacts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
