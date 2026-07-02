namespace Slotra.Api.DTOs.Staff;

public sealed record StaffServiceResponse(Guid ServiceId, string Name, int DurationMinutes, decimal Price, bool IsActive);
