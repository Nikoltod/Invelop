namespace Invelop.Contacts.Application.Tests;

/// <summary>A <see cref="TimeProvider"/> frozen at a fixed date, pinned to UTC.</summary>
internal sealed class FixedTimeProvider(DateOnly today) : TimeProvider
{
    public static readonly DateOnly DefaultToday = new(2026, 11, 19);

    public static TimeProvider Default { get; } = new FixedTimeProvider(DefaultToday);

    public override DateTimeOffset GetUtcNow() => new(today.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
