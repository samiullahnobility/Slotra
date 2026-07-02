namespace Slotra.Api.DTOs.Appointments;

public sealed record AppointmentQueryRequest(
    string? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? StaffId,
    Guid? ServiceId,
    int Page = 1,
    int PageSize = 25);
