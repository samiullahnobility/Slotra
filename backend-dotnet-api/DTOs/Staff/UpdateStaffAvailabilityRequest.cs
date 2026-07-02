namespace Slotra.Api.DTOs.Staff;

public sealed record UpdateStaffAvailabilityRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, bool IsActive);
