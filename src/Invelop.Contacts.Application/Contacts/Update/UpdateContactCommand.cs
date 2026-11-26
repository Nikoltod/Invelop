namespace Invelop.Contacts.Application.Contacts.Update;

public sealed record UpdateContactCommand(
    string? FirstName,
    string? Surname,
    DateOnly? DateOfBirth,
    string? Address,
    string? PhoneNumber,
    string? Iban);
