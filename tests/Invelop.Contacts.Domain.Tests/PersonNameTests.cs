namespace Invelop.Contacts.Domain.Tests;

public class PersonNameTests
{
    [Theory]
    [InlineData("Ada", "Lovelace")]
    [InlineData("Siobhán", "O'Brien")]          // non-ASCII letter and an apostrophe
    [InlineData("Jean-Luc", "Picard")]          // hyphenated
    [InlineData("Maria", "Ruiz de la Peña")]    // internal spaces
    [InlineData("J.", "Wu")]                    // initial with a period
    public void Accepts_ordinary_names(string first, string surname)
    {
        var name = PersonName.Create(first, surname);

        Assert.Equal(first, name.First);
        Assert.Equal(surname, name.Surname);
    }

    [Fact]
    public void Trims_and_collapses_internal_whitespace()
    {
        var name = PersonName.Create("  Ada   Augusta ", "  Lovelace  ");

        Assert.Equal("Ada Augusta", name.First);
        Assert.Equal("Lovelace", name.Surname);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ada2")]        // digits are the thing being excluded
    [InlineData("<script>")]
    [InlineData("'Ada")]        // must start with a letter
    public void Rejects_invalid_parts(string? part)
    {
        Assert.False(PersonName.IsValidPart(part));
    }

    [Fact]
    public void Rejects_a_part_over_the_length_limit()
    {
        Assert.True(PersonName.IsValidPart(new string('a', ContactRules.MaxNameLength)));
        Assert.False(PersonName.IsValidPart(new string('a', ContactRules.MaxNameLength + 1)));
    }

    [Fact]
    public void Reports_which_field_failed()
    {
        var ex = Assert.Throws<DomainValidationException>(() => PersonName.Create("Ada", "Lovelace9"));
        Assert.Equal("surname", ex.PropertyName);
    }
}
