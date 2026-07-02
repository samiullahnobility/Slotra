namespace Slotra.Api.DTOs.Booking;

public sealed record BookingServiceResponse(Guid Id, string Name, string? Description, int DurationMinutes, decimal Price);
