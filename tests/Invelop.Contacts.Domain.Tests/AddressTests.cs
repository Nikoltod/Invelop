namespace Invelop.Contacts.Domain.Tests;

public class AddressTests
{
    [Fact]
    public void Preserves_intended_line_breaks()
    {
        var address = Address.Create("Keizersgracht 123\n1015 CJ Amsterdam\nNetherlands");

        Assert.Equal("Keizersgracht 123\n1015 CJ Amsterdam\nNetherlands", address.Value);
        Assert.Equal(3, address.LineCount);
    }

    [Fact]
    public void Normalises_line_endings_trims_lines_and_drops_blank_ones()
    {
        var address = Address.Create("  Keizersgracht 123  \r\n\r\n  1015 CJ Amsterdam \r\n\n");

        Assert.Equal("Keizersgracht 123\n1015 CJ Amsterdam", address.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\n\n")]
    public void Requires_a_value(string? raw)
    {
        Assert.False(Address.IsWellFormed(raw));
        Assert.Throws<DomainValidationException>(() => Address.Create(raw));
    }

    [Fact]
    public void Rejects_an_address_over_the_length_limit()
    {
        Assert.True(Address.IsWellFormed(new string('a', ContactRules.MaxAddressLength)));
        Assert.False(Address.IsWellFormed(new string('a', ContactRules.MaxAddressLength + 1)));
    }

    [Fact]
    public void Rejects_more_lines_than_allowed()
    {
        var atLimit = string.Join('\n', Enumerable.Repeat("line", ContactRules.MaxAddressLines));
        var overLimit = string.Join('\n', Enumerable.Repeat("line", ContactRules.MaxAddressLines + 1));

        Assert.True(Address.IsWellFormed(atLimit));
        Assert.False(Address.IsWellFormed(overLimit));
    }

    [Fact]
    public void Normalisation_is_idempotent()
    {
        // Matters for the UI: if a round-trip through the API changed the value, the edit form
        // would come back dirty the moment it loaded.
        const string messy = " a \r\n\r\n b ";
        Assert.Equal(Address.Normalize(messy), Address.Normalize(Address.Normalize(messy)));
    }
}
