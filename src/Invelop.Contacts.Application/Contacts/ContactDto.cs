using Invelop.Contacts.Domain;

namespace Invelop.Contacts.Application.Contacts;

/// <summary>
/// The wire representation of a contact.
/// </summary>
/// <remarks>
/// Flat and primitive on purpose: the value objects are a domain concern, and the client should
/// not have to understand them. <see cref="DateOnly"/> serialises as <c>"1990-05-17"</c> with no
/// time and no offset, which is what keeps a date of birth from drifting a day across timezones.
/// </remarks>
public sealed record ContactDto(
    Guid Id,
    string FirstName,
    string Surname,
    DateOnly DateOfBirth,
    string Address,
    string PhoneNumber,
    string Iban);

public static class ContactDtoMapping
{
    /// <summary>
    /// Maps an already-materialised aggregate, for the write path which has one in hand.
    /// The read path does not use this - it projects straight to <see cref="ContactDto"/> in SQL.
    /// </summary>
    public static ContactDto ToDto(this Contact contact) => new(
        contact.Id,
        contact.Name.First,
        contact.Name.Surname,
        contact.DateOfBirth,
        contact.Address.Value,
        contact.PhoneNumber.Value,
        contact.Iban.Value);
}
