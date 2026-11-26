using Invelop.Contacts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Invelop.Contacts.Infrastructure;

/// <summary>
/// Maps the <see cref="Contact"/> aggregate onto one flat table.
/// </summary>
/// <remarks>
/// The value objects are mapped as EF <b>complex properties</b>, not value converters. The
/// difference matters for the read side: a complex property gives each member its own column, so
/// <c>OrderBy(c =&gt; c.Name.Surname)</c> and the DTO projection in <c>GetContactsHandler</c>
/// translate into SQL. A converter would collapse each value object into one opaque string and
/// force sorting and projection to happen in memory after loading every row.
/// <para>
/// EF binds the value objects' private constructors directly, which is what lets a row that is
/// invalid on disk still load. That is intentional - see <c>AppDbContext.SaveChangesAsync</c>.
/// </para>
/// </remarks>
public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    /// <summary>The longest IBAN in the ISO 13616 registry.</summary>
    private const int MaxIbanLength = 34;

    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(contact => contact.Id);

        // Ids come from Guid.CreateVersion7() in the domain factory, not from the database.
        builder.Property(contact => contact.Id).ValueGeneratedNever();

        builder.Property(contact => contact.DateOfBirth).IsRequired();

        builder.ComplexProperty(contact => contact.Name, name =>
        {
            name.Property(personName => personName.First)
                .HasColumnName("FirstName")
                .HasMaxLength(ContactRules.MaxNameLength)
                .IsRequired();

            name.Property(personName => personName.Surname)
                .HasColumnName("Surname")
                .HasMaxLength(ContactRules.MaxNameLength)
                .IsRequired();
        });

        builder.ComplexProperty(contact => contact.Address, address =>
        {
            address.Property(value => value.Value)
                .HasColumnName("Address")
                .HasMaxLength(ContactRules.MaxAddressLength)
                .IsRequired();

            // Derived from Value; there is no column behind it.
            address.Ignore(value => value.LineCount);
        });

        builder.ComplexProperty(contact => contact.PhoneNumber, phoneNumber =>
        {
            phoneNumber.Property(value => value.Value)
                .HasColumnName("PhoneNumber")
                .HasMaxLength(ContactRules.MaxPhoneDigits + 1) // +1 for the leading '+'
                .IsRequired();
        });

        builder.ComplexProperty(contact => contact.Iban, iban =>
        {
            iban.Property(value => value.Value)
                .HasColumnName("Iban")
                .HasMaxLength(MaxIbanLength)
                .IsRequired();

            // Derived from Value; there is no column behind it.
            iban.Ignore(value => value.CountryCode);
        });
    }
}
