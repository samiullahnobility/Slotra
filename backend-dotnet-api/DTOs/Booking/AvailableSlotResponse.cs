namespace Slotra.Api.DTOs.Booking;

public sealed record AvailableSlotResponse(Guid StaffProfileId, string StaffDisplayName, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
