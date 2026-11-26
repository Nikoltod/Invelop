using Invelop.Contacts.Domain;

namespace Invelop.Contacts.Application.Contacts;

/// <summary>
/// The user-facing wording for each rule, in one place.
/// </summary>
/// <remarks>
/// The <em>predicates</em> are shared by living in the domain. These are the <em>messages</em>,
/// shared for the same reason: the command validator and the entity validator both reach for
/// them, so a reviewer reading either one sees the same sentence a user would see. Lengths are
/// interpolated from <see cref="ContactRules"/> rather than typed out, so changing a limit
/// changes the message too.
/// </remarks>
public static class ContactValidationMessages
{
    public const string FirstNameRequired = "First name is required.";
    public const string SurnameRequired = "Surname is required.";

    public static readonly string FirstNameInvalid =
        $"First name must be at most {ContactRules.MaxNameLength} characters and cannot contain digits.";

    public static readonly string SurnameInvalid =
        $"Surname must be at most {ContactRules.MaxNameLength} characters and cannot contain digits.";

    public const string AddressRequired = "Address is required.";

    public static readonly string AddressInvalid =
        $"Address must be at most {ContactRules.MaxAddressLength} characters and at most "
            + $"{ContactRules.MaxAddressLines} lines.";

    public const string DateOfBirthRequired = "Date of birth is required.";

    public static readonly string DateOfBirthInvalid = DateOfBirthRules.DescribeRule();

    public const string PhoneNumberRequired = "Phone number is required.";

    public static readonly string PhoneNumberInvalid =
        $"Enter a phone number with {ContactRules.MinPhoneDigits} to {ContactRules.MaxPhoneDigits} "
            + "digits, optionally starting with '+' or '00'.";

    public const string IbanRequired = "IBAN is required.";

    public const string IbanInvalid =
        "Enter a valid IBAN. Check the country code, the length, and for any mistyped characters.";
}
