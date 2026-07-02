namespace Slotra.Api.DTOs.Staff;

public sealed record StaffAvailabilityResponse(Guid Id, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, bool IsActive);
