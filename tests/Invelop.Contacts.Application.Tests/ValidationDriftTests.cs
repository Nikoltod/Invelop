using FluentValidation;
using Invelop.Contacts.Application.Contacts.Create;
using Invelop.Contacts.Domain;

namespace Invelop.Contacts.Application.Tests;

/// <summary>
/// The anti-drift guarantee, as an executable check.
/// </summary>
/// <remarks>
/// The claim this design rests on is that the boundary validator and the domain enforce the same
/// rules because they call the same predicates. A claim like that is worth exactly as much as the
/// test that holds it to account: every input below is fed to <em>both</em>, and both must reject
/// it. Loosen a rule in one place and this goes red.
/// </remarks>
public class ValidationDriftTests
{
    private static readonly IValidator<CreateContactCommand> Validator =
        new CreateContactCommandValidator(FixedTimeProvider.Default);

    private static readonly CreateContactCommand Valid = new(
        "Ada",
        "Lovelace",
        new DateOnly(1990, 5, 17),
        "Keizersgracht 123",
        "+31201234567",
        "NL91ABNA0417164300");

    [Theory]
    [InlineData("FirstName", null)]
    [InlineData("FirstName", "")]
    [InlineData("FirstName", "Ada2")]
    [InlineData("FirstName", "<script>")]
    [InlineData("Surname", null)]
    [InlineData("Surname", "Lovelace9")]
    [InlineData("Address", null)]
    [InlineData("Address", "")]
    [InlineData("PhoneNumber", null)]
    [InlineData("PhoneNumber", "CALL ME")]
    [InlineData("PhoneNumber", "1234567")]            // one digit short
    [InlineData("Iban", null)]
    [InlineData("Iban", "NL91ABNA0417164030")]        // valid structure, transposed digits
    [InlineData("Iban", "ZZ91ABNA0417164300")]        // unregistered country
    [InlineData("Iban", "NL91ABNA041716430")]         // wrong length for NL
    public void The_boundary_and_the_domain_reject_the_same_inputs(string field, string? value)
    {
        var result = Validator.Validate(CommandWith(field, value));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == field);

        Assert.Throws<DomainValidationException>(() => ConstructViaDomain(field, value));
    }

    [Fact]
    public void Both_accept_a_valid_command()
    {
        Assert.True(Validator.Validate(Valid).IsValid);

        var contact = Contact.Create(
            PersonName.Create(Valid.FirstName, Valid.Surname),
            Valid.DateOfBirth!.Value,
            Address.Create(Valid.Address),
            PhoneNumber.Create(Valid.PhoneNumber),
            Iban.Create(Valid.Iban),
            FixedTimeProvider.Default);

        Assert.Equal("NL91ABNA0417164300", contact.Iban.Value);
    }

    [Fact]
    public void A_single_request_reports_every_bad_field_at_once()
    {
        // One round trip, not six. This is the boundary validator's real job - the domain throws
        // on the first problem it meets, which is correct for an invariant and useless for a form.
        var result = Validator.Validate(
            new CreateContactCommand("Ada2", "", new DateOnly(2099, 1, 1), "", "CALL ME", "nope"));

        Assert.Equal(
            ["FirstName", "Surname", "DateOfBirth", "Address", "PhoneNumber", "Iban"],
            result.Errors.Select(error => error.PropertyName).Distinct());
    }

    [Fact]
    public void The_date_of_birth_rule_is_the_same_on_both_sides()
    {
        var future = FixedTimeProvider.DefaultToday.AddDays(1);

        Assert.False(Validator.Validate(Valid with { DateOfBirth = future }).IsValid);

        Assert.Throws<DomainValidationException>(() => Contact.Create(
            PersonName.Create("Ada", "Lovelace"),
            future,
            Address.Create("Keizersgracht 123"),
            PhoneNumber.Create("+31201234567"),
            Iban.Create("NL91ABNA0417164300"),
            FixedTimeProvider.Default));
    }

    private static CreateContactCommand CommandWith(string field, string? value) => field switch
    {
        "FirstName" => Valid with { FirstName = value },
        "Surname" => Valid with { Surname = value },
        "Address" => Valid with { Address = value },
        "PhoneNumber" => Valid with { PhoneNumber = value },
        "Iban" => Valid with { Iban = value },
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unmapped field."),
    };

    private static object ConstructViaDomain(string field, string? value) => field switch
    {
        "FirstName" => PersonName.Create(value, Valid.Surname),
        "Surname" => PersonName.Create(Valid.FirstName, value),
        "Address" => Address.Create(value),
        "PhoneNumber" => PhoneNumber.Create(value),
        "Iban" => Iban.Create(value),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unmapped field."),
    };
}
