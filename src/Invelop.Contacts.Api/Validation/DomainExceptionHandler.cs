using Invelop.Contacts.Application.Abstractions;
using Invelop.Contacts.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace Invelop.Contacts.Api.Validation;

/// <summary>
/// Translates the two "this should never happen" validation failures into a 400 the UI can use,
/// while recording them as warnings for us.
/// </summary>
/// <remarks>
/// Reaching either of these means boundary validation let something through that the domain or
/// the database layer then rejected - in other words, the layers have drifted. The user still
/// gets a sensible, field-mapped error rather than a 500, but the log line is the signal that a
/// rule needs fixing. An unhandled exception here would be the wrong answer in both directions:
/// a crash for the user, and silence for us.
/// </remarks>
public sealed class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    private const string Title = "One or more validation errors occurred.";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string[]> errors;

        switch (exception)
        {
            case DomainValidationException domainException:
                logger.LogWarning(
                    domainException,
                    "A domain invariant rejected input that boundary validation accepted ({Property}). "
                        + "The command validator and the domain have drifted apart.",
                    domainException.PropertyName);
                errors = ValidationProblemFactory.SingleError(
                    domainException.PropertyName,
                    domainException.Message);
                break;

            case EntityValidationException entityException:
                logger.LogWarning(
                    entityException,
                    "Entity validation rejected a write at SaveChanges. Either a persisted row is "
                        + "invalid, or a code path reached the database without going through the "
                        + "domain factories.");
                errors = ValidationProblemFactory.ToErrorDictionary(entityException.Errors);
                break;

            default:
                return false;
        }

        await TypedResults.ValidationProblem(errors, title: Title).ExecuteAsync(httpContext);

        return true;
    }
}
