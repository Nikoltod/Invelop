using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Invelop.Contacts.Domain;

/// <summary>
/// A phone number stored in a canonical digits-only form, optionally prefixed with '+'.
/// </summary>
/// <remarks>
/// This validates <em>shape</em>, not reachability. Without a country context there is no honest
/// way to tell a valid national number from an invalid one, and pretending otherwise would reject
/// legitimate numbers. The rule is therefore the E.164 envelope: an optional '+' and 8-15 digits.
/// </remarks>
public sealed partial record PhoneNumber
{
    // Built from ContactRules rather than source-generated, so the digit bounds have exactly one
    // definition. An interpolated [GeneratedRegex] is not a constant expression when the holes
    // are ints, and duplicating "8,15" into an attribute is precisely the drift this design avoids.
    private static readonly Regex EnvelopePattern = new(
        $@"^\+?[0-9]{{{ContactRules.MinPhoneDigits},{ContactRules.MaxPhoneDigits}}}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private PhoneNumber(string value) => Value = value;

    public string Value { get; }

    public static bool IsWellFormed([NotNullWhen(true)] string? raw) => TryNormalize(raw, out _);

    /// <summary>
    /// Strips the separators people actually type - spaces, hyphens, dots, parentheses - and
    /// rewrites a leading international '00' prefix as '+'. Returns false if what remains is not
    /// a plausible number. (.NET's <c>\s</c> is Unicode-aware, so non-breaking spaces pasted from
    /// a web page are stripped too.)
    /// </summary>
    public static bool TryNormalize([NotNullWhen(true)] string? raw, [NotNullWhen(true)] out string? normalized)
    {
        normalized = null;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var stripped = Separators().Replace(raw.Trim(), string.Empty);

        if (stripped.StartsWith("00", StringComparison.Ordinal))
        {
            stripped = string.Concat("+", stripped.AsSpan(2));
        }

        if (!EnvelopePattern.IsMatch(stripped))
        {
            return false;
        }

        normalized = stripped;
        return true;
    }

    public static PhoneNumber Create(string? raw)
    {
        if (!TryNormalize(raw, out var normalized))
        {
            throw new DomainValidationException(
                nameof(raw),
                $"Phone number is required and must contain {ContactRules.MinPhoneDigits} to "
                    + $"{ContactRules.MaxPhoneDigits} digits, optionally starting with '+' or '00'.");
        }

        return new PhoneNumber(normalized);
    }

    /// <summary>Rehydrates a persisted row without validating. EF Core only.</summary>
    internal static PhoneNumber FromStorage(string value) => new(value);

    public override string ToString() => Value;

    [GeneratedRegex(@"[\s\-.()]", RegexOptions.CultureInvariant)]
    private static partial Regex Separators();
}
