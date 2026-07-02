using Slotra.Api.Common;
using Slotra.Api.DTOs.Appointments;
using Slotra.Api.DTOs.Booking;

namespace Slotra.Api.Services;

public interface IBookingService
{
    Task<IReadOnlyList<BookingServiceResponse>> GetServicesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailableStaffResponse>> GetAvailableStaffAsync(Guid serviceId, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<AvailableSlotResponse>>> GetAvailableSlotsAsync(Guid serviceId, DateOnly date, Guid? staffId, CancellationToken cancellationToken = default);

    Task<ServiceResult<PagedResponse<AppointmentResponse>>> GetAppointmentsAsync(Guid userId, IReadOnlyCollection<string> roles, AppointmentQueryRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AppointmentResponse>> GetMyAppointmentsAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<AppointmentResponse>>> GetStaffTodayAppointmentsAsync(Guid staffUserId, CancellationToken cancellationToken = default);

    Task<ServiceResult<AppointmentResponse>> CreateAppointmentAsync(Guid customerId, IReadOnlyCollection<string> roles, CreateAppointmentRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> CancelAppointmentAsync(Guid appointmentId, Guid userId, IReadOnlyCollection<string> roles, CancelAppointmentRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, Guid userId, IReadOnlyCollection<string> roles, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<AppointmentResponse>> UpdateStatusAsync(Guid appointmentId, Guid userId, IReadOnlyCollection<string> roles, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult<IReadOnlyList<AppointmentNoteResponse>>> GetNotesAsync(Guid appointmentId, Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default);

    Task<ServiceResult<AppointmentNoteResponse>> AddNoteAsync(Guid appointmentId, Guid authorId, IReadOnlyCollection<string> roles, CreateAppointmentNoteRequest request, CancellationToken cancellationToken = default);
}

