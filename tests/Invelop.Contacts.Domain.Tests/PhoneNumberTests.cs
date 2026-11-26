namespace Invelop.Contacts.Domain.Tests;

public class PhoneNumberTests
{
    [Theory]
    [InlineData("+31 20 123 4567", "+31201234567")]
    [InlineData("+31-20-123-4567", "+31201234567")]
    [InlineData("(020) 123 4567", "0201234567")]
    [InlineData("020.123.4567", "0201234567")]
    [InlineData("0031201234567", "+31201234567")]  // international 00 prefix becomes +
    [InlineData("  +31201234567  ", "+31201234567")]
    public void Normalises_the_separators_people_actually_type(string raw, string expected)
    {
        Assert.True(PhoneNumber.TryNormalize(raw, out var normalized));
        Assert.Equal(expected, normalized);
        Assert.Equal(expected, PhoneNumber.Create(raw).Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234567")]                 // 7 digits, one short of the minimum
    [InlineData("1234567890123456")]        // 16 digits, one over E.164
    [InlineData("+31 20 CALL ME")]          // letters are not separators
    [InlineData("+31+20+1234567")]          // '+' is only meaningful as a prefix
    public void Rejects_implausible_numbers(string? raw)
    {
        Assert.False(PhoneNumber.TryNormalize(raw, out _));
        Assert.False(PhoneNumber.IsWellFormed(raw));
    }

    [Theory]
    [InlineData("12345678")]                // exactly the 8-digit minimum
    [InlineData("+123456789012345")]        // exactly the 15-digit maximum
    public void Accepts_the_boundaries(string raw)
    {
        Assert.True(PhoneNumber.IsWellFormed(raw));
    }

    [Fact]
    public void Create_rejects_a_number_the_predicate_also_rejects()
    {
        Assert.Throws<DomainValidationException>(() => PhoneNumber.Create("nope"));
    }
}
