namespace Invelop.Contacts.Domain;

/// <summary>
/// The shared date-of-birth predicate, used by the command validator, the entity validator and
/// <see cref="Contact"/> itself.
/// </summary>
/// <remarks>
/// "Today" is always passed in rather than read from <see cref="DateTime"/>. A rule whose
/// outcome depends on the wall clock is untestable at its boundaries otherwise - and the
/// boundaries (a birthday of exactly today, or exactly 120 years ago) are the cases worth testing.
/// </remarks>
public static class DateOfBirthRules
{
    public static bool IsValid(DateOnly dateOfBirth, DateOnly today) =>
        dateOfBirth <= today && dateOfBirth >= today.AddYears(-ContactRules.MaxAgeInYears);

    public static DateOnly Today(TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

    public static string DescribeRule() =>
        $"Date of birth must not be in the future or more than {ContactRules.MaxAgeInYears} years ago.";
}
