using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Invelop.Contacts.Domain;

/// <summary>
/// An International Bank Account Number, stored uppercase with no spaces.
/// </summary>
/// <remarks>
/// Validation is the real ISO 13616 / ISO 7064 check, not a regex that merely looks convincing:
/// structure, then the country's registered length, then the mod-97 checksum. A single
/// transposed digit - the most common way to mistype an IBAN - fails the checksum, which is the
/// entire reason the check digits exist. Catching that at entry is worth more to a user than any
/// other rule in this application.
/// <para>
/// IBANs are deliberately <b>not</b> unique across contacts: joint accounts and family members
/// sharing an account are ordinary. See README "Design Decisions".
/// </para>
/// </remarks>
public sealed partial record Iban
{
    /// <summary>
    /// Registered IBAN lengths per ISO 13616. Checking the length for the specific country
    /// rejects a whole class of truncated or padded input that a generic 15-34 range would wave
    /// through, even when the checksum happens to pass.
    /// </summary>
    private static readonly Dictionary<string, int> LengthByCountry = new(StringComparer.Ordinal)
    {
        ["AD"] = 24, ["AE"] = 23, ["AL"] = 28, ["AT"] = 20, ["AZ"] = 28, ["BA"] = 20,
        ["BE"] = 16, ["BG"] = 22, ["BH"] = 22, ["BI"] = 27, ["BR"] = 29, ["BY"] = 28,
        ["CH"] = 21, ["CR"] = 22, ["CY"] = 28, ["CZ"] = 24, ["DE"] = 22, ["DJ"] = 27,
        ["DK"] = 18, ["DO"] = 28, ["EE"] = 20, ["EG"] = 29, ["ES"] = 24, ["FI"] = 18,
        ["FO"] = 18, ["FR"] = 27, ["GB"] = 22, ["GE"] = 22, ["GI"] = 23, ["GL"] = 18,
        ["GR"] = 27, ["GT"] = 28, ["HR"] = 21, ["HU"] = 28, ["IE"] = 22, ["IL"] = 23,
        ["IQ"] = 23, ["IS"] = 26, ["IT"] = 27, ["JO"] = 30, ["KW"] = 30, ["KZ"] = 20,
        ["LB"] = 28, ["LC"] = 32, ["LI"] = 21, ["LT"] = 20, ["LU"] = 20, ["LV"] = 21,
        ["LY"] = 25, ["MC"] = 27, ["MD"] = 24, ["ME"] = 22, ["MK"] = 19, ["MR"] = 27,
        ["MT"] = 31, ["MU"] = 30, ["NI"] = 28, ["NL"] = 18, ["NO"] = 15, ["PK"] = 24,
        ["PL"] = 28, ["PS"] = 29, ["PT"] = 25, ["QA"] = 29, ["RO"] = 24, ["RS"] = 22,
        ["RU"] = 33, ["SA"] = 24, ["SC"] = 31, ["SD"] = 18, ["SE"] = 24, ["SI"] = 19,
        ["SK"] = 24, ["SM"] = 27, ["SO"] = 23, ["ST"] = 25, ["SV"] = 28, ["TL"] = 23,
        ["TN"] = 24, ["TR"] = 26, ["UA"] = 29, ["VA"] = 22, ["VG"] = 24, ["XK"] = 20,
    };

    private Iban(string value) => Value = value;

    /// <summary>The canonical form: uppercase, no spaces. This is what is persisted.</summary>
    public string Value { get; }

    public string CountryCode => Value[..2];

    /// <summary>
    /// The shared predicate, called by the FluentValidation command validator so the boundary
    /// and the domain can never disagree about what a valid IBAN is.
    /// </summary>
    public static bool IsWellFormed([NotNullWhen(true)] string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = Normalize(raw);

        return Structure().IsMatch(normalized)
            && LengthByCountry.TryGetValue(normalized[..2], out var expectedLength)
            && normalized.Length == expectedLength
            && Mod97(normalized) == 1;
    }

    /// <summary>Uppercases and removes all whitespace. Accepts the grouped form users paste.</summary>
    public static string Normalize(string raw) =>
        Whitespace().Replace(raw, string.Empty).ToUpperInvariant();

    /// <summary>Formats as groups of four for display, the way a bank statement prints it.</summary>
    public static string ToDisplayString(string normalized) =>
        string.Join(' ', normalized.Chunk(4).Select(chunk => new string(chunk)));

    public static Iban Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new DomainValidationException(nameof(raw), "IBAN is required.");
        }

        var normalized = Normalize(raw);

        if (!Structure().IsMatch(normalized))
        {
            throw new DomainValidationException(
                nameof(raw),
                "IBAN must start with two letters and two digits, followed by up to 30 letters or digits.");
        }

        if (!LengthByCountry.TryGetValue(normalized[..2], out var expectedLength))
        {
            throw new DomainValidationException(
                nameof(raw),
                $"'{normalized[..2]}' is not a recognised IBAN country code.");
        }

        if (normalized.Length != expectedLength)
        {
            throw new DomainValidationException(
                nameof(raw),
                $"An IBAN for {normalized[..2]} must be {expectedLength} characters, but this one is {normalized.Length}.");
        }

        if (Mod97(normalized) != 1)
        {
            throw new DomainValidationException(
                nameof(raw),
                "IBAN checksum is not valid - please check for a mistyped or transposed character.");
        }

        return new Iban(normalized);
    }

    /// <summary>Rehydrates a persisted row without validating. EF Core only.</summary>
    internal static Iban FromStorage(string value) => new(value);

    public override string ToString() => Value;

    /// <summary>
    /// ISO 7064 MOD-97-10. Moves the first four characters to the end, maps letters to two-digit
    /// numbers (A=10 ... Z=35), and takes the remainder mod 97; a valid IBAN leaves 1.
    /// </summary>
    /// <remarks>
    /// Folded digit by digit rather than materialising the ~40-digit number, which would overflow
    /// every built-in integer type and otherwise force a BigInteger allocation per check.
    /// </remarks>
    private static int Mod97(string normalized)
    {
        var remainder = 0;

        for (var i = 0; i < normalized.Length; i++)
        {
            // Rotate: start at index 4 and wrap the first four characters onto the end.
            var c = normalized[(i + 4) % normalized.Length];

            remainder = char.IsAsciiDigit(c)
                ? (remainder * 10 + (c - '0')) % 97
                : (remainder * 100 + (c - 'A' + 10)) % 97;
        }

        return remainder;
    }

    [GeneratedRegex(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$", RegexOptions.CultureInvariant)]
    private static partial Regex Structure();

    [GeneratedRegex(@"\s", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
