namespace Invelop.Contacts.Domain.Tests;

public class IbanTests
{
    // Published ISO 13616 example IBANs. These are documentation samples, not real accounts.
    [Theory]
    [InlineData("NL91ABNA0417164300")]
    [InlineData("DE89370400440532013000")]
    [InlineData("GB82WEST12345698765432")]
    [InlineData("BE68539007547034")]
    [InlineData("FR1420041010050500013M02606")]
    [InlineData("CH9300762011623852957")]
    public void Accepts_valid_ibans(string value)
    {
        Assert.True(Iban.IsWellFormed(value));
        Assert.Equal(value, Iban.Create(value).Value);
    }

    [Theory]
    [InlineData("nl91 abna 0417 1643 00", "NL91ABNA0417164300")]
    [InlineData("  DE89 3704 0044 0532 0130 00  ", "DE89370400440532013000")]
    public void Normalises_case_and_spacing_on_the_way_in(string raw, string expected)
    {
        Assert.Equal(expected, Iban.Create(raw).Value);
    }

    [Fact]
    public void Rejects_a_transposed_digit()
    {
        // NL91ABNA0417164300 with the final two digits swapped. Structure and length are still
        // correct, so only the checksum can catch this - which is the whole point of mod-97.
        Assert.False(Iban.IsWellFormed("NL91ABNA0417164030"));
    }

    [Fact]
    public void Rejects_a_length_that_is_wrong_for_the_country()
    {
        // Valid Dutch IBAN with one character lopped off.
        Assert.False(Iban.IsWellFormed("NL91ABNA041716430"));
    }

    [Fact]
    public void Rejects_an_unregistered_country_code()
    {
        Assert.False(Iban.IsWellFormed("ZZ91ABNA0417164300"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-iban")]
    [InlineData("1234567890123456")]
    public void Rejects_malformed_input(string? value)
    {
        Assert.False(Iban.IsWellFormed(value));
    }

    [Fact]
    public void Create_reports_the_checksum_failure_distinctly_from_a_structural_one()
    {
        var checksum = Assert.Throws<DomainValidationException>(() => Iban.Create("NL91ABNA0417164030"));
        Assert.Contains("checksum", checksum.Message, StringComparison.OrdinalIgnoreCase);

        var structural = Assert.Throws<DomainValidationException>(() => Iban.Create("1234"));
        Assert.DoesNotContain("checksum", structural.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Formats_for_display_in_groups_of_four()
    {
        Assert.Equal("NL91 ABNA 0417 1643 00", Iban.ToDisplayString("NL91ABNA0417164300"));
    }

    [Fact]
    public void Equality_is_by_value()
    {
        Assert.Equal(Iban.Create("nl91 abna 0417 1643 00"), Iban.Create("NL91ABNA0417164300"));
    }
}
