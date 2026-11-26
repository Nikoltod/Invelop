using FluentValidation;

namespace Invelop.Contacts.Api.Validation;

/// <summary>
/// Runs the FluentValidation validator for <typeparamref name="TRequest"/> before the endpoint's
/// handler, short-circuiting with a 400 if it fails.
/// </summary>
/// <remarks>
/// The only generic machinery in this codebase, and it is ASP.NET's own
/// <see cref="IEndpointFilter"/> extension point rather than something hand-rolled. It earns its
/// place by keeping the response shape - the status code, the ProblemDetails envelope, the
/// camel-cased keys - defined once, so every write endpoint reports failures identically and the
/// client has a single error contract to parse.
/// <para>
/// Both lookups below use <c>GetRequiredService</c> and an explicit throw: forgetting to register
/// a validator should fail loudly on the first request in development, not silently wave invalid
/// input through to the handler in production.
/// </para>
/// </remarks>
public sealed class ValidationFilter<TRequest> : IEndpointFilter
    where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"ValidationFilter<{typeof(TRequest).Name}> is attached to an endpoint that takes no "
                    + $"{typeof(TRequest).Name} argument.");

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<TRequest>>();

        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);

        return result.IsValid
            ? await next(context)
            : TypedResults.ValidationProblem(ValidationProblemFactory.ToErrorDictionary(result.Errors));
    }
}

public static class ValidationFilterExtensions
{
    public static RouteHandlerBuilder ValidateRequest<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class =>
        builder.AddEndpointFilter<ValidationFilter<TRequest>>().ProducesValidationProblem();
}
