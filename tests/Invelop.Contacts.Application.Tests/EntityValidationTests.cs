using Invelop.Contacts.Application.Abstractions;
using Invelop.Contacts.Application.Contacts.Create;
using Invelop.Contacts.Domain;
using Microsoft.EntityFrameworkCore;

namespace Invelop.Contacts.Application.Tests;

/// <summary>
/// Demonstrates what entity-level validation at SaveChanges catches that neither the command
/// validator nor the domain factories can.
/// </summary>
/// <remarks>
/// This is the test that justifies the third validation layer existing at all. Without a case
/// like this one, "FluentValidation for entities" is a box ticked to satisfy the brief; with it,
/// the layer has a demonstrated job.
/// </remarks>
public class EntityValidationTests
{
    private static readonly CreateContactCommand ValidCommand = new(
        "Ada",
        "Lovelace",
        new DateOnly(1990, 5, 17),
        "Keizersgracht 123",
        "+31201234567",
        "NL91ABNA0417164300");

    [Fact]
    public async Task Refuses_to_rewrite_a_row_that_was_corrupted_outside_the_application()
    {
        await using var database = await TestDatabase.CreateAsync(FixedTimeProvider.Default);

        var handler = new CreateContactHandler(database.Context, FixedTimeProvider.Default);
        var created = await handler.Handle(ValidCommand, CancellationToken.None);

        // Something outside EF Core writes a broken IBAN: a hand-run UPDATE, a restored backup,
        // a bad data migration, another service sharing the schema. The domain factory cannot
        // help here - it never runs on this path.
        //
        // COLLATE NOCASE because EF Core persists Guid keys as *uppercase* TEXT while
        // Guid.ToString() is lowercase, and SQLite compares text case-sensitively by default.
        // Without it this UPDATE silently matches zero rows and the test passes for the wrong
        // reason - which is exactly what it did the first time it was run.
        var rowsAffected = await database.ExecuteRawSqlAsync(
            $"UPDATE Contacts SET Iban = 'DEFINITELY-NOT-AN-IBAN' WHERE Id = '{created.Id}' COLLATE NOCASE");

        Assert.Equal(1, rowsAffected);

        // A fresh context, so the contact is materialised from the corrupted row rather than
        // served from the first context's change tracker.
        await using var freshContext = database.NewContext(FixedTimeProvider.Default);

        var contact = await freshContext.Contacts.SingleAsync(CancellationToken.None);

        // It loaded without complaint - that is the point. EF bound the constructor directly,
        // so every guard in the domain was bypassed.
        Assert.Equal("DEFINITELY-NOT-AN-IBAN", contact.Iban.Value);

        // Now edit something unrelated, exactly as a user correcting a typo in the address would.
        contact.MoveTo(Address.Create("Rue de la Loi 16"));

        var exception = await Assert.ThrowsAsync<EntityValidationException>(
            () => freshContext.SaveChangesAsync(CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "Iban");

        // The corrupt value was not re-written, and the legitimate edit was not silently applied.
        await using var verifyContext = database.NewContext(FixedTimeProvider.Default);
        var unchanged = await verifyContext.Contacts.SingleAsync(CancellationToken.None);
        Assert.Equal("Keizersgracht 123", unchanged.Address.Value);
    }

    [Fact]
    public async Task Lets_a_legitimate_edit_through_untouched()
    {
        await using var database = await TestDatabase.CreateAsync(FixedTimeProvider.Default);

        var handler = new CreateContactHandler(database.Context, FixedTimeProvider.Default);
        var created = await handler.Handle(ValidCommand, CancellationToken.None);

        await using var editContext = database.NewContext(FixedTimeProvider.Default);
        var contact = await editContext.Contacts.SingleAsync(CancellationToken.None);
        contact.MoveTo(Address.Create("Rue de la Loi 16\n1000 Brussels"));
        await editContext.SaveChangesAsync(CancellationToken.None);

        await using var verifyContext = database.NewContext(FixedTimeProvider.Default);
        var saved = await verifyContext.Contacts.SingleAsync(CancellationToken.None);

        Assert.Equal(created.Id, saved.Id);
        Assert.Equal("Rue de la Loi 16\n1000 Brussels", saved.Address.Value);
    }
}
