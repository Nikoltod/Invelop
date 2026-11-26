using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Invelop.Contacts.Domain;

/// <summary>
/// A contact's first name and surname, trimmed and whitespace-collapsed.
/// </summary>
public sealed partial record PersonName
{
    private PersonName(string first, string surname)
    {
        First = first;
        Surname = surname;
    }

    public string First { get; }

    public string Surname { get; }

    /// <summary>
    /// The shared predicate. The FluentValidation command validator calls this for both
    /// FirstName and Surname rather than restating the rule, so the two cannot drift.
    /// </summary>
    public static bool IsValidPart([NotNullWhen(true)] string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var normalized = Normalize(raw);
        return normalized.Length <= ContactRules.MaxNameLength && NamePattern().IsMatch(normalized);
    }

    /// <summary>Trims and collapses runs of internal whitespace to a single space.</summary>
    public static string Normalize(string raw) => WhitespaceRuns().Replace(raw.Trim(), " ");

    public static PersonName Create(string? first, string? surname)
    {
        if (!IsValidPart(first))
        {
            throw new DomainValidationException(
                nameof(first),
                $"First name is required, must be at most {ContactRules.MaxNameLength} characters, and cannot contain digits.");
        }

        if (!IsValidPart(surname))
        {
            throw new DomainValidationException(
                nameof(surname),
                $"Surname is required, must be at most {ContactRules.MaxNameLength} characters, and cannot contain digits.");
        }

        return new PersonName(Normalize(first), Normalize(surname));
    }

    /// <summary>Rehydrates a persisted row without validating. EF Core only.</summary>
    internal static PersonName FromStorage(string first, string surname) => new(first, surname);

    public override string ToString() => $"{First} {Surname}";

    // A letter, then letters, combining marks, spaces, dashes, apostrophes and periods.
    // Deliberately Unicode-aware: "Ortega", "O'Brien", "Jean-Luc" and "Ruiz de la Peña" are
    // all ordinary names. Digits are the thing being excluded.
    [GeneratedRegex(@"^\p{L}[\p{L}\p{M}\p{Pd}'’. ]*$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRuns();
}
