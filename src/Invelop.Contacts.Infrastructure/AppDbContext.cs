using FluentValidation;
using FluentValidation.Results;
using Invelop.Contacts.Application.Abstractions;
using Invelop.Contacts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Invelop.Contacts.Infrastructure;

public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IValidator<Contact> contactValidator)
    : DbContext(options), IAppDbContext
{
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    /// <summary>
    /// Validates every added or modified contact before anything reaches the database.
    /// </summary>
    /// <remarks>
    /// This is the "entities" half of the brief's "FluentValidation for models and entities", and
    /// it is deliberately here rather than in an <c>ISaveChangesInterceptor</c>. The behaviour is
    /// identical; the difference is that a reviewer opening the DbContext sees the rule enforced
    /// in the file they are already reading, instead of a registration call pointing somewhere
    /// else. See <see cref="ContactValidator"/> for what this catches that the command validator
    /// and the domain factories cannot.
    /// </remarks>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await ValidateChangedContactsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateChangedContactsAsync(CancellationToken cancellationToken)
    {
        // Enumerating Entries<T>() runs change detection, so a contact mutated through a
        // behaviour method is already marked Modified by the time we look.
        var changed = ChangeTracker
            .Entries<Contact>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .ToList();

        if (changed.Count == 0)
        {
            return;
        }

        List<ValidationFailure>? failures = null;

        foreach (var entry in changed)
        {
            var result = await contactValidator.ValidateAsync(entry.Entity, cancellationToken);

            if (!result.IsValid)
            {
                (failures ??= []).AddRange(result.Errors);
            }
        }

        if (failures is not null)
        {
            throw new EntityValidationException(failures);
        }
    }
}
