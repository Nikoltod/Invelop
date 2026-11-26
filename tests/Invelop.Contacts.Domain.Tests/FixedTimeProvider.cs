namespace Invelop.Contacts.Domain.Tests;

/// <summary>
/// A <see cref="TimeProvider"/> frozen at a fixed instant, pinned to UTC so the date-of-birth
/// boundary tests give the same answer regardless of where the build runs.
/// </summary>
internal sealed class FixedTimeProvider(DateOnly today) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
