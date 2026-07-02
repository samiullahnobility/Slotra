namespace Slotra.Api.Services;

public sealed class BusinessClock(IConfiguration configuration) : IBusinessClock
{
    public TimeZoneInfo TimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById(configuration["BusinessTimeZone"] ?? "America/Phoenix");

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, TimeZone).DateTime);

    public DateTimeOffset LocalDateTimeToUtc(DateOnly date, TimeOnly time)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, TimeZone), TimeSpan.Zero);
    }

    public DateTimeOffset[] LocalDateRangeToUtc(DateOnly date)
    {
        var start = LocalDateTimeToUtc(date, TimeOnly.MinValue);
        var end = LocalDateTimeToUtc(date.AddDays(1), TimeOnly.MinValue);
        return [start, end];
    }

    public (DateOnly Date, TimeOnly Time) UtcToLocalParts(DateTimeOffset utcDateTime)
    {
        var local = TimeZoneInfo.ConvertTime(utcDateTime, TimeZone);
        return (DateOnly.FromDateTime(local.DateTime), TimeOnly.FromDateTime(local.DateTime));
    }
}
