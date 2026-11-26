namespace Invelop.Contacts.Domain;

/// <summary>
/// Thrown when an attempt is made to construct or mutate a domain object into an invalid state.
/// </summary>
/// <remarks>
/// In normal operation this is never thrown: boundary validation rejects bad input first and
/// returns a friendly, field-mapped 400. If one of these reaches the API it means the boundary
/// validator and the domain have drifted apart, which is why the API logs it as a warning
/// rather than treating it as routine.
/// <para>
/// <see cref="PropertyName"/> carries the field so the API can still map it onto the right
/// form control instead of degrading to a generic error.
/// </para>
/// </remarks>
public sealed class DomainValidationException(string propertyName, string message)
    : Exception(message)
{
    public string PropertyName { get; } = propertyName;
}
