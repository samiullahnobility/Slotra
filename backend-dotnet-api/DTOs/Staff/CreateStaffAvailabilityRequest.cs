namespace Slotra.Api.DTOs.Staff;

public sealed record CreateStaffAvailabilityRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, bool IsActive = true);
