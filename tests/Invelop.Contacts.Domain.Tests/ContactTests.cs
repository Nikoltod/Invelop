namespace Invelop.Contacts.Domain.Tests;

public class ContactTests
{
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly TimeProvider Clock = new FixedTimeProvider(Today);

    private static Contact CreateContact(DateOnly? dateOfBirth = null) => Contact.Create(
        PersonName.Create("Ada", "Lovelace"),
        dateOfBirth ?? new DateOnly(1990, 5, 17),
        Address.Create("Keizersgracht 123\n1015 CJ Amsterdam"),
        PhoneNumber.Create("+31 20 123 4567"),
        Iban.Create("NL91 ABNA 0417 1643 00"),
        Clock);

    [Fact]
    public void Create_assigns_an_id_and_the_normalised_values()
    {
        var contact = CreateContact();

        Assert.NotEqual(Guid.Empty, contact.Id);
        Assert.Equal("Ada", contact.Name.First);
        Assert.Equal("Lovelace", contact.Name.Surname);
        Assert.Equal(new DateOnly(1990, 5, 17), contact.DateOfBirth);
        Assert.Equal("+31201234567", contact.PhoneNumber.Value);
        Assert.Equal("NL91ABNA0417164300", contact.Iban.Value);
    }

    [Fact]
    public void Create_assigns_a_fresh_id_each_time()
    {
        Assert.NotEqual(CreateContact().Id, CreateContact().Id);
    }

    [Fact]
    public void Rejects_a_date_of_birth_in_the_future()
    {
        var ex = Assert.Throws<DomainValidationException>(() => CreateContact(Today.AddDays(1)));
        Assert.Equal("dateOfBirth", ex.PropertyName);
    }

    [Fact]
    public void Accepts_a_date_of_birth_of_today()
    {
        // A contact born today is unusual but not invalid, and off-by-one here is the easiest
        // mistake to make in this rule.
        Assert.Equal(Today, CreateContact(Today).DateOfBirth);
    }

    [Fact]
    public void Accepts_the_oldest_allowed_date_and_rejects_one_day_earlier()
    {
        var oldest = Today.AddYears(-ContactRules.MaxAgeInYears);

        Assert.Equal(oldest, CreateContact(oldest).DateOfBirth);
        Assert.Throws<DomainValidationException>(() => CreateContact(oldest.AddDays(-1)));
    }

    [Fact]
    public void Behaviour_methods_change_the_aggregate()
    {
        var contact = CreateContact();

        contact.Rename(PersonName.Create("Augusta", "King"));
        contact.MoveTo(Address.Create("Rue de la Loi 16\n1000 Brussels"));
        contact.ChangePhoneNumber(PhoneNumber.Create("0032 2 123 4567"));
        contact.ChangeIban(Iban.Create("BE68539007547034"));
        contact.CorrectDateOfBirth(new DateOnly(1991, 1, 1), Clock);

        Assert.Equal("Augusta King", contact.Name.ToString());
        Assert.Equal("Rue de la Loi 16\n1000 Brussels", contact.Address.Value);
        Assert.Equal("+3221234567", contact.PhoneNumber.Value);
        Assert.Equal("BE68539007547034", contact.Iban.Value);
        Assert.Equal(new DateOnly(1991, 1, 1), contact.DateOfBirth);
    }

    [Fact]
    public void Correcting_the_date_of_birth_applies_the_same_rule_as_creation()
    {
        var contact = CreateContact();

        Assert.Throws<DomainValidationException>(
            () => contact.CorrectDateOfBirth(Today.AddDays(1), Clock));

        // The failed change must not have been applied.
        Assert.Equal(new DateOnly(1990, 5, 17), contact.DateOfBirth);
    }
}
