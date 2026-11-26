using System.Text.Json;
using FluentValidation.Results;

namespace Invelop.Contacts.Api.Validation;

/// <summary>
/// Turns validation failures into the <c>errors</c> dictionary of an RFC 9457 response.
/// </summary>
public static class ValidationProblemFactory
{
    /// <remarks>
    /// The keys are run through the <em>same</em> naming policy that serialises the DTOs, so an
    /// error key is always spelled exactly like the JSON field it refers to - and therefore
    /// exactly like the Angular form control bound to it.
    /// <para>
    /// This is the detail that decides whether server-side validation feels finished. FluentValidation
    /// reports <c>"Iban"</c>, the form control is named <c>iban</c>, and without this conversion
    /// every server error would quietly miss its field and fall through to the page-level banner.
    /// Deriving it from <see cref="JsonNamingPolicy.CamelCase"/> rather than hand-rolling a
    /// lowercase-the-first-letter helper means it cannot disagree with the serialiser later.
    /// </para>
    /// </remarks>
    public static Dictionary<string, string[]> ToErrorDictionary(IEnumerable<ValidationFailure> failures) =>
        failures
            .GroupBy(failure => ToFieldName(failure.PropertyName), StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

    public static Dictionary<string, string[]> SingleError(string propertyName, string message) =>
        new(StringComparer.Ordinal) { [ToFieldName(propertyName)] = [message] };

    private static string ToFieldName(string propertyName) =>
        string.IsNullOrEmpty(propertyName)
            ? string.Empty
            : JsonNamingPolicy.CamelCase.ConvertName(propertyName);
}
