namespace Invelop.Contacts.Domain;

/// <summary>
/// The single source of truth for every size and range limit on a contact.
/// </summary>
/// <remarks>
/// These constants are referenced by three layers: the FluentValidation command validators at
/// the API boundary, the value-object factories in this assembly, and the entity validator that
/// runs at SaveChanges. None of those layers re-states a number; they all read it from here.
/// That is what keeps the "models and entities" validation from drifting apart.
/// <para>
/// The format predicates themselves live next to the type they describe:
/// <see cref="PersonName.IsValidPart"/>, <see cref="Address.IsWellFormed"/>,
/// <see cref="PhoneNumber.TryNormalize"/>, <see cref="Iban.IsWellFormed"/>.
/// </para>
/// </remarks>
public static class ContactRules
{
    public const int MaxNameLength = 50;

    public const int MaxAddressLength = 250;
    public const int MaxAddressLines = 5;

    public const int MinPhoneDigits = 8;
    public const int MaxPhoneDigits = 15;

    /// <summary>Oldest plausible living person, used as the lower bound on date of birth.</summary>
    public const int MaxAgeInYears = 120;
}
