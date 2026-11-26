namespace Invelop.Contacts.Application.Contacts.Create;

/// <summary>
/// The request to create a contact, exactly as it arrives on the wire.
/// </summary>
/// <remarks>
/// Every member is nullable so that a missing field reaches the validator and comes back as
/// "First name is required" against the right form control. If these were non-nullable, model
/// binding would quietly substitute defaults - an absent date of birth would arrive as
/// 0001-01-01 and be reported as "more than 120 years ago", which is true but useless.
/// </remarks>
public sealed record CreateContactCommand(
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    string? Address,
    string? PhoneNumber,
    string? Iban);
