namespace Slotra.Api.DTOs.Appointments;

public sealed record RescheduleAppointmentRequest(Guid StaffProfileId, DateTimeOffset StartsAt);
