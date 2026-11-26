using System.Diagnostics.CodeAnalysis;

namespace Invelop.Contacts.Domain;

/// <summary>
/// A postal address held as one required multiline block of text.
/// </summary>
/// <remarks>
/// Deliberately unstructured. The brief lists "address" as a single field, and a structured
/// street/postcode/city/country model would impose a format that does not survive contact with
/// international addresses. Normalisation is limited to trimming lines and dropping blank ones,
/// so line breaks the user intended are preserved.
/// </remarks>
public sealed record Address
{
    private Address(string value) => Value = value;

    public string Value { get; }

    /// <summary>Line count after normalisation, exposed so the UI can size the textarea.</summary>
    public int LineCount => Value.Split('\n').Length;

    public static bool IsWellFormed([NotNullWhen(true)] string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = Normalize(raw);
        return normalized.Length is > 0 and <= ContactRules.MaxAddressLength
            && normalized.Split('\n').Length <= ContactRules.MaxAddressLines;
    }

    /// <summary>
    /// Normalises line endings to <c>\n</c>, trims each line, and drops blank lines. Storing a
    /// single canonical form means a round-trip through the UI cannot silently change the value
    /// and mark the form dirty.
    /// </summary>
    public static string Normalize(string raw) => string.Join(
        '\n',
        raw.ReplaceLineEndings("\n")
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0));

    public static Address Create(string? raw)
    {
        if (!IsWellFormed(raw))
        {
            throw new DomainValidationException(
                nameof(raw),
                $"Address is required, must be at most {ContactRules.MaxAddressLength} characters, "
                    + $"and at most {ContactRules.MaxAddressLines} lines.");
        }

        return new Address(Normalize(raw));
    }

    /// <summary>Rehydrates a persisted row without validating. EF Core only.</summary>
    internal static Address FromStorage(string value) => new(value);

    public override string ToString() => Value;
}
