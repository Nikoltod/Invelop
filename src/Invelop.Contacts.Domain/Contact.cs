namespace Invelop.Contacts.Domain;

/// <summary>
/// A personal contact. The aggregate root, and the only entity in this domain.
/// </summary>
/// <remarks>
/// There is no public constructor and there are no public setters. Every way of bringing a
/// <see cref="Contact"/> into existence or changing one runs through a method on this class,
/// which means an invalid instance cannot be constructed - not by a handler, not by a seeder,
/// not by a test, and not by code written six months from now.
/// <para>
/// The one exception is EF Core materialisation, which bypasses all of this by design. That is
/// exactly the hole the entity validator at SaveChanges exists to cover; see
/// <c>AppDbContext.SaveChangesAsync</c>.
/// </para>
/// </remarks>
public sealed class Contact
{
    // EF Core materialisation. The provider assigns every property immediately afterwards, so
    // the nulls below are never observable; `null!` keeps that promise explicit rather than
    // disabling nullable analysis for the whole type.
    private Contact()
    {
        Name = null!;
        Address = null!;
        PhoneNumber = null!;
        Iban = null!;
    }

    private Contact(
        Guid id,
        PersonName name,
        DateOnly dateOfBirth,
        Address address,
        PhoneNumber phoneNumber,
        Iban iban)
    {
        Id = id;
        Name = name;
        DateOfBirth = dateOfBirth;
        Address = address;
        PhoneNumber = phoneNumber;
        Iban = iban;
    }

    public Guid Id { get; private set; }

    public PersonName Name { get; private set; }

    public DateOnly DateOfBirth { get; private set; }

    public Address Address { get; private set; }

    public PhoneNumber PhoneNumber { get; private set; }

    public Iban Iban { get; private set; }

    /// <summary>
    /// The only way to create a contact. Takes already-constructed value objects, so by the time
    /// this runs the name, address, phone and IBAN have each validated themselves - leaving this
    /// method responsible for just the one invariant that spans the aggregate.
    /// </summary>
    public static Contact Create(
        PersonName name,
        DateOnly dateOfBirth,
        Address address,
        PhoneNumber phoneNumber,
        Iban iban,
        TimeProvider timeProvider)
    {
        GuardDateOfBirth(dateOfBirth, timeProvider);

        return new Contact(Guid.CreateVersion7(), name, dateOfBirth, address, phoneNumber, iban);
    }

    public void Rename(PersonName name) => Name = name;

    public void CorrectDateOfBirth(DateOnly dateOfBirth, TimeProvider timeProvider)
    {
        GuardDateOfBirth(dateOfBirth, timeProvider);
        DateOfBirth = dateOfBirth;
    }

    public void MoveTo(Address address) => Address = address;

    public void ChangePhoneNumber(PhoneNumber phoneNumber) => PhoneNumber = phoneNumber;

    public void ChangeIban(Iban iban) => Iban = iban;

    private static void GuardDateOfBirth(DateOnly dateOfBirth, TimeProvider timeProvider)
    {
        if (!DateOfBirthRules.IsValid(dateOfBirth, DateOfBirthRules.Today(timeProvider)))
        {
            throw new DomainValidationException(nameof(dateOfBirth), DateOfBirthRules.DescribeRule());
        }
    }
}
