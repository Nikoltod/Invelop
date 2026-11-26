using FluentValidation.Results;

namespace Invelop.Contacts.Application.Abstractions;

/// <summary>
/// Thrown when an entity fails validation at persist time.
/// </summary>
/// <remarks>
/// Distinct from FluentValidation's own <c>ValidationException</c> so the API can tell the two
/// apart. A boundary failure is routine and expected; one of these means a write got past both
/// the command validator and the domain factory, so the API maps it to a 400 for the user but
/// logs it as a warning for us.
/// </remarks>
public sealed class EntityValidationException(IReadOnlyList<ValidationFailure> errors)
    : Exception(BuildMessage(errors))
{
    public IReadOnlyList<ValidationFailure> Errors { get; } = errors;

    private static string BuildMessage(IReadOnlyList<ValidationFailure> errors) =>
        "An entity failed validation while being saved: "
            + string.Join("; ", errors.Select(error => $"{error.PropertyName}: {error.ErrorMessage}"));
}
