using Invelop.Contacts.Application.Contacts.Create;
using Invelop.Contacts.Application.Contacts.Delete;
using Invelop.Contacts.Application.Contacts.Get;
using Invelop.Contacts.Application.Contacts.GetAll;
using Invelop.Contacts.Application.Contacts.Update;

namespace Invelop.Contacts.Application.Tests;

public class ContactHandlerTests
{
    [Fact]
    public async Task Create_persists_the_normalised_values_not_the_raw_input()
    {
        await using var database = await TestDatabase.CreateAsync(FixedTimeProvider.Default);

        var created = await new CreateContactHandler(database.Context, FixedTimeProvider.Default).Handle(
            new CreateContactCommand(
                "  Ada  ",
                "Lovelace",
                new DateOnly(1990, 5, 17),
                "  Keizersgracht 123 \r\n\r\n 1015 CJ Amsterdam ",
                "+31 (20) 123-4567",
                "nl91 abna 0417 1643 00"),
            CancellationToken.None);

        Assert.Equal("Ada", created.FirstName);
        Assert.Equal("Keizersgracht 123\n1015 CJ Amsterdam", created.Address);
        Assert.Equal("+31201234567", created.PhoneNumber);
        Assert.Equal("NL91ABNA0417164300", created.Iban);

        // And the normalised form is what actually reached the database, not just the response.
        await using var verifyContext = database.NewContext(FixedTimeProvider.Default);
        var listed = await new GetContactsHandler(verifyContext).Handle(CancellationToken.None);

        Assert.Equal(created, Assert.Single(listed));
    }

    [Fact]
    public async Task Create_round_trips_the_date_of_birth_exactly()
    {
        // DateOnly all the way down: no time component to be shifted by a timezone anywhere
        // between the form and the SQLite TEXT column.
        var dateOfBirth = new DateOnly(1990, 5, 17);

        await using var database = await TestDatabase.CreateAsync(FixedTimeProvider.Default);

        var created = await new CreateContactHandler(database.Context, FixedTimeProvider.Default).Handle(
            new CreateContactCommand("Ada", "Lovelace", dateOfBirth, "Somewhere", "+31201234567", "NL91ABNA0417164300"),
            CancellationToken.None);

        await using var verifyContext = database.NewContext(FixedTimeProvider.Default);
        var listed = await new GetContactsHandler(verifyContext).Handle(CancellationToken.None);

        Assert.Equal(dateOfBirth, created.DateOfBirth);
        Assert.Equal(dateOfBirth, listed[0].DateOfBirth);
    }

    [Fact]
    public async Task List_is_ordered_by_surname_then_first_name()
    {
        await using var database = await TestDatabase.CreateAsync(FixedTimeProvider.Default);
        var handler = new CreateContactHandler(database.Context, FixedTimeProvider.Default);

        foreach (var (first, surname) in new[] { ("Zoe", "Young"), ("Bob", "Adams"), ("Alice", "Adams") })
        {
            await handler.Handle(
                new CreateContactCommand(first, surname, new DateOnly(1990, 1, 1), "Somewhere", "+31201234567", "NL91ABNA0417164300"),
                CancellationToken.None);
        }

        await using var verifyContext = database.NewContext(FixedTimeProvider.Default);
        var listed = await new GetContactsHandler(verifyContext).Handle(CancellationToken.None);

        Assert.Equal(
            ["Alice Adams", "Bob Adams", "Zoe Young"],
            listed.Select(contact => $"{contact.FirstName} {contact.Surname}"));
    }

    [Fact]
    public async Task List_is_empty_before_anything_is_created()
    {
        await using var database = await TestDatabase.CreateAsync(FixedTimeProvider.Default);

        Assert.Empty(await new GetContactsHandler(database.Context).Handle(CancellationToken.None));
    }

    [Fact]
    public async Task Two_contacts_may_share_an_iban()
    {
        // An explicit product decision, not an oversight: joint accounts and family members
        // sharing an account are ordinary. Pinned by a test so nobody "fixes" it by adding a
        // unique index. See README "Design Decisions".
        await using var database = await TestDatabase.CreateAsync(FixedTimeProvider.Default);
        var handler = new CreateContactHandler(database.Context, FixedTimeProvider.Default);

        await handler.Handle(
            new CreateContactCommand("Ada", "Lovelace", new DateOnly(1990, 1, 1), "Somewhere", "+31201234567", "NL91ABNA0417164300"),
            CancellationToken.None);
        await handler.Handle(
            new CreateContactCommand("Charles", "Babbage", new DateOnly(1991, 1, 1), "Somewhere", "+31201234568", "NL91ABNA0417164300"),
            CancellationToken.None);

        await using var verifyContext = database.NewContext(FixedTimeProvider.Default);
        var listed = await new GetContactsHandler(verifyContext).Handle(CancellationToken.None);

        Assert.Equal(2, listed.Count);
        Assert.All(listed, contact => Assert.Equal("NL91ABNA0417164300", contact.Iban));
    }

    [Fact]
    public async Task Get_update_and_delete_round_trip()
    {
        await using var database = await TestDatabase.CreateAsync(FixedTimeProvider.Default);

        var created = await new CreateContactHandler(database.Context, FixedTimeProvider.Default).Handle(
            new CreateContactCommand("Ada", "Lovelace", new DateOnly(1990, 5, 17), "London", "+31201234567", "NL91ABNA0417164300"),
            CancellationToken.None);

        var loaded = await new GetContactHandler(database.Context).Handle(created.Id, CancellationToken.None);
        Assert.Equal(created, loaded);

        var updated = await new UpdateContactHandler(database.Context, FixedTimeProvider.Default).Handle(
            created.Id,
            new UpdateContactCommand("Augusta", "King", new DateOnly(1991, 1, 1), "London 2", "0032 2 123 4567", "BE68539007547034"),
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Augusta", updated!.FirstName);
        Assert.Equal("BE68539007547034", updated.Iban);

        Assert.True(await new DeleteContactHandler(database.Context).Handle(created.Id, CancellationToken.None));
        Assert.Null(await new GetContactHandler(database.Context).Handle(created.Id, CancellationToken.None));
    }
}
