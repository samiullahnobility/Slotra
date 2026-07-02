namespace Slotra.Api.Services;

public interface IBusinessClock
{
    TimeZoneInfo TimeZone { get; }
    DateOnly Today { get; }
    DateTimeOffset UtcNow { get; }
    DateTimeOffset LocalDateTimeToUtc(DateOnly date, TimeOnly time);
    DateTimeOffset[] LocalDateRangeToUtc(DateOnly date);
    (DateOnly Date, TimeOnly Time) UtcToLocalParts(DateTimeOffset utcDateTime);
}
