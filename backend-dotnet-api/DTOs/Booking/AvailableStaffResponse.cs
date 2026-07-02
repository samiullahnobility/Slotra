namespace Slotra.Api.DTOs.Booking;

public sealed record AvailableStaffResponse(Guid StaffProfileId, string DisplayName, string? Bio);
