namespace Slotra.Api.DTOs.Appointments;

public sealed record CreateAppointmentRequest(Guid ServiceId, Guid StaffProfileId, DateTimeOffset StartsAt);
