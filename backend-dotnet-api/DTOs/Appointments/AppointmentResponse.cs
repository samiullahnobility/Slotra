namespace Slotra.Api.DTOs.Appointments;

public sealed record AppointmentResponse(
    Guid Id,
    Guid CustomerId,
    Guid StaffProfileId,
    string StaffDisplayName,
    Guid ServiceId,
    string ServiceName,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Status);
