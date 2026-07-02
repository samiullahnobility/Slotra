namespace Slotra.Api.DTOs.Appointments;

public sealed record AppointmentNoteResponse(Guid Id, Guid AppointmentId, Guid AuthorId, string AuthorDisplayName, string Body, DateTimeOffset CreatedAt);
