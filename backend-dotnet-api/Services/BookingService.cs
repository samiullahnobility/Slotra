using Microsoft.EntityFrameworkCore;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Appointments;
using Slotra.Api.DTOs.Booking;
using Slotra.Api.Models;
using Slotra.Api.UnitOfWork;

namespace Slotra.Api.Services;

public sealed class BookingService(IUnitOfWork unitOfWork, IBusinessClock businessClock) : IBookingService
{
    private static readonly AppointmentStatus[] BlockingStatuses = [AppointmentStatus.Confirmed];

    public async Task<IReadOnlyList<BookingServiceResponse>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        return await unitOfWork.Repository<Service>()
            .Query()
            .Where(service => service.IsActive)
            .OrderBy(service => service.Name)
            .Select(service => new BookingServiceResponse(service.Id, service.Name, service.Description, service.DurationMinutes, service.Price))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AvailableStaffResponse>> GetAvailableStaffAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        return await unitOfWork.Repository<StaffService>()
            .Query()
            .Where(staffService => staffService.ServiceId == serviceId && staffService.IsActive && staffService.StaffProfile.IsActive && staffService.Service.IsActive)
            .Include(staffService => staffService.StaffProfile)
            .ThenInclude(profile => profile.User)
            .OrderBy(staffService => staffService.StaffProfile.User.DisplayName)
            .Select(staffService => new AvailableStaffResponse(staffService.StaffProfileId, staffService.StaffProfile.User.DisplayName, staffService.StaffProfile.Bio))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<IReadOnlyList<AvailableSlotResponse>>> GetAvailableSlotsAsync(Guid serviceId, DateOnly date, Guid? staffId, CancellationToken cancellationToken = default)
    {
        var service = await unitOfWork.Repository<Service>().GetByIdAsync(serviceId, cancellationToken);
        if (service is null || !service.IsActive)
        {
            return ServiceResult<IReadOnlyList<AvailableSlotResponse>>.NotFound("Service was not found.");
        }

        var staffQuery = unitOfWork.Repository<StaffService>()
            .Query()
            .Where(staffService => staffService.ServiceId == serviceId && staffService.IsActive && staffService.StaffProfile.IsActive)
            .Include(staffService => staffService.StaffProfile)
            .ThenInclude(profile => profile.User)
            .AsQueryable();

        if (staffId.HasValue)
        {
            staffQuery = staffQuery.Where(staffService => staffService.StaffProfileId == staffId.Value);
        }

        var staffServices = await staffQuery.ToListAsync(cancellationToken);
        if (staffId.HasValue && staffServices.Count == 0)
        {
            return ServiceResult<IReadOnlyList<AvailableSlotResponse>>.NotFound("Staff member was not found for this service.");
        }

        var dayOfWeek = date.DayOfWeek;
        var range = businessClock.LocalDateRangeToUtc(date);
        var dayStart = range[0];
        var dayEnd = range[1];

        var appointments = await unitOfWork.Repository<Appointment>()
            .Query()
            .Where(appointment => appointment.ServiceId == serviceId && appointment.StartsAt < dayEnd && appointment.EndsAt > dayStart && BlockingStatuses.Contains(appointment.Status))
            .ToListAsync(cancellationToken);

        var slots = new List<AvailableSlotResponse>();
        foreach (var staffService in staffServices)
        {
            var availability = await unitOfWork.Repository<StaffAvailability>()
                .Query()
                .Where(item => item.StaffProfileId == staffService.StaffProfileId && item.DayOfWeek == dayOfWeek && item.IsActive)
                .ToListAsync(cancellationToken);

            var staffAppointments = appointments.Where(appointment => appointment.StaffProfileId == staffService.StaffProfileId).ToList();

            foreach (var window in availability)
            {
                var cursor = businessClock.LocalDateTimeToUtc(date, window.StartTime);
                var windowEnd = businessClock.LocalDateTimeToUtc(date, window.EndTime);

                while (cursor.AddMinutes(service.DurationMinutes) <= windowEnd)
                {
                    var slotEnd = cursor.AddMinutes(service.DurationMinutes);
                    var overlaps = staffAppointments.Any(appointment => cursor < appointment.EndsAt && slotEnd > appointment.StartsAt);

                    if (!overlaps)
                    {
                        slots.Add(new AvailableSlotResponse(staffService.StaffProfileId, staffService.StaffProfile.User.DisplayName, cursor, slotEnd));
                    }

                    cursor = cursor.AddMinutes(service.DurationMinutes);
                }
            }
        }

        return ServiceResult<IReadOnlyList<AvailableSlotResponse>>.Success(slots.OrderBy(slot => slot.StartsAt).ThenBy(slot => slot.StaffDisplayName).ToList());
    }

    public async Task<ServiceResult<PagedResponse<AppointmentResponse>>> GetAppointmentsAsync(Guid userId, IReadOnlyCollection<string> roles, AppointmentQueryRequest request, CancellationToken cancellationToken = default)
    {
        var query = AppointmentQuery();

        if (roles.Contains(RoleNames.Admin))
        {
            return await ToPagedAppointmentsAsync(query, request, cancellationToken);
        }

        if (roles.Contains(RoleNames.Staff))
        {
            query = query.Where(appointment => appointment.StaffProfile.UserId == userId);
        }
        else
        {
            query = query.Where(appointment => appointment.CustomerId == userId);
        }

        return await ToPagedAppointmentsAsync(query, request, cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentResponse>> GetMyAppointmentsAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await AppointmentQuery()
            .Where(appointment => appointment.CustomerId == customerId)
            .OrderByDescending(appointment => appointment.StartsAt)
            .Select(appointment => ToResponse(appointment))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<IReadOnlyList<AppointmentResponse>>> GetStaffTodayAppointmentsAsync(Guid staffUserId, CancellationToken cancellationToken = default)
    {
        var staff = await unitOfWork.Repository<StaffProfile>()
            .Query()
            .SingleOrDefaultAsync(profile => profile.UserId == staffUserId, cancellationToken);

        if (staff is null)
        {
            return ServiceResult<IReadOnlyList<AppointmentResponse>>.NotFound("Staff profile was not found.");
        }

        var range = businessClock.LocalDateRangeToUtc(businessClock.Today);
        var start = range[0];
        var end = range[1];

        var appointments = await AppointmentQuery()
            .Where(appointment => appointment.StaffProfileId == staff.Id && appointment.StartsAt >= start && appointment.StartsAt < end)
            .OrderBy(appointment => appointment.StartsAt)
            .Select(appointment => ToResponse(appointment))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<AppointmentResponse>>.Success(appointments);
    }

    public async Task<ServiceResult<AppointmentResponse>> CreateAppointmentAsync(Guid customerId, IReadOnlyCollection<string> roles, CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        if (!roles.Contains(RoleNames.Customer) && !roles.Contains(RoleNames.Admin))
        {
            return ServiceResult<AppointmentResponse>.ValidationError("Only customers can create appointments.");
        }

        var startsAt = request.StartsAt.ToUniversalTime();
        if (startsAt <= businessClock.UtcNow)
        {
            return ServiceResult<AppointmentResponse>.ValidationError("Appointment start time must be in the future.");
        }

        var service = await unitOfWork.Repository<Service>().GetByIdAsync(request.ServiceId, cancellationToken);
        if (service is null || !service.IsActive)
        {
            return ServiceResult<AppointmentResponse>.NotFound("Service was not found.");
        }

        var staffService = await unitOfWork.Repository<StaffService>()
            .Query()
            .Include(item => item.StaffProfile)
            .ThenInclude(profile => profile.User)
            .SingleOrDefaultAsync(item => item.StaffProfileId == request.StaffProfileId && item.ServiceId == request.ServiceId, cancellationToken);

        if (staffService is null || !staffService.StaffProfile.IsActive)
        {
            return ServiceResult<AppointmentResponse>.NotFound("Staff member was not found for this service.");
        }

        var endsAt = startsAt.AddMinutes(service.DurationMinutes);

        if (!await IsWithinAvailabilityAsync(request.StaffProfileId, startsAt, endsAt, cancellationToken))
        {
            return ServiceResult<AppointmentResponse>.ValidationError("Selected time is outside staff availability.");
        }

        if (await HasStaffOverlapAsync(request.StaffProfileId, startsAt, endsAt, null, cancellationToken))
        {
            return ServiceResult<AppointmentResponse>.Conflict("Selected time is no longer available.");
        }

        var customer = await unitOfWork.Repository<AppUser>().GetByIdAsync(customerId, cancellationToken);
        var appointment = new Appointment
        {
            CustomerId = customerId,
            Customer = customer!,
            StaffProfileId = request.StaffProfileId,
            StaffProfile = staffService.StaffProfile,
            ServiceId = request.ServiceId,
            Service = service,
            StartsAt = startsAt,
            EndsAt = endsAt,
            Status = AppointmentStatus.Confirmed
        };

        await unitOfWork.Repository<Appointment>().AddAsync(appointment, cancellationToken);
        await unitOfWork.Repository<Notification>().AddAsync(CreateNotification(customer, appointment, "BookingConfirmation", "Appointment confirmed", $"Your {service.Name} appointment has been confirmed for {startsAt:u}."), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AppointmentResponse>.Success(ToResponse(appointment));
    }

    public async Task<ServiceResult> CancelAppointmentAsync(Guid appointmentId, Guid userId, IReadOnlyCollection<string> roles, CancelAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var appointment = await GetAppointmentForMutationAsync(appointmentId, cancellationToken);
        if (appointment is null || !CanManageAppointment(appointment, userId, roles))
        {
            return ServiceResult.NotFound();
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            return ServiceResult.Conflict("Appointment is already cancelled.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.CancelledAt = DateTimeOffset.UtcNow;
        appointment.CancellationReason = request.Reason?.Trim();

        unitOfWork.Repository<Appointment>().Update(appointment);
        await unitOfWork.Repository<Notification>().AddAsync(CreateNotification(appointment.Customer, appointment, "AppointmentCancellation", "Appointment cancelled", $"Your {appointment.Service.Name} appointment has been cancelled."), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, Guid userId, IReadOnlyCollection<string> roles, RescheduleAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var appointment = await GetAppointmentForMutationAsync(appointmentId, cancellationToken);
        if (appointment is null || !CanManageAppointment(appointment, userId, roles))
        {
            return ServiceResult<AppointmentResponse>.NotFound();
        }

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            return ServiceResult<AppointmentResponse>.Conflict("Only confirmed appointments can be rescheduled.");
        }

        var staffService = await unitOfWork.Repository<StaffService>()
            .Query()
            .Include(item => item.StaffProfile)
            .ThenInclude(profile => profile.User)
            .SingleOrDefaultAsync(item => item.StaffProfileId == request.StaffProfileId && item.ServiceId == appointment.ServiceId, cancellationToken);

        if (staffService is null || !staffService.StaffProfile.IsActive)
        {
            return ServiceResult<AppointmentResponse>.NotFound("Staff member was not found for this service.");
        }

        var startsAt = request.StartsAt.ToUniversalTime();
        if (startsAt <= businessClock.UtcNow)
        {
            return ServiceResult<AppointmentResponse>.ValidationError("Appointment start time must be in the future.");
        }

        var endsAt = startsAt.AddMinutes(appointment.Service.DurationMinutes);

        if (!await IsWithinAvailabilityAsync(request.StaffProfileId, startsAt, endsAt, cancellationToken))
        {
            return ServiceResult<AppointmentResponse>.ValidationError("Selected time is outside staff availability.");
        }

        if (await HasStaffOverlapAsync(request.StaffProfileId, startsAt, endsAt, appointment.Id, cancellationToken))
        {
            return ServiceResult<AppointmentResponse>.Conflict("Selected time is no longer available.");
        }

        appointment.StaffProfileId = request.StaffProfileId;
        appointment.StaffProfile = staffService.StaffProfile;
        appointment.StartsAt = startsAt;
        appointment.EndsAt = endsAt;

        unitOfWork.Repository<Appointment>().Update(appointment);
        await unitOfWork.Repository<Notification>().AddAsync(CreateNotification(appointment.Customer, appointment, "AppointmentRescheduled", "Appointment rescheduled", $"Your {appointment.Service.Name} appointment has been rescheduled for {startsAt:u}."), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AppointmentResponse>.Success(ToResponse(appointment));
    }

    public async Task<ServiceResult<AppointmentResponse>> UpdateStatusAsync(Guid appointmentId, Guid userId, IReadOnlyCollection<string> roles, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken = default)
    {
        var appointment = await GetAppointmentForMutationAsync(appointmentId, cancellationToken);
        if (appointment is null || !CanManageAppointment(appointment, userId, roles))
        {
            return ServiceResult<AppointmentResponse>.NotFound();
        }

        if (!Enum.TryParse<AppointmentStatus>(request.Status, true, out var status))
        {
            return ServiceResult<AppointmentResponse>.ValidationError("Appointment status is invalid.");
        }

        if (!roles.Contains(RoleNames.Admin) && status is not (AppointmentStatus.Completed or AppointmentStatus.NoShow))
        {
            return ServiceResult<AppointmentResponse>.ValidationError("Staff can only mark appointments as Completed or NoShow.");
        }

        appointment.Status = status;
        if (status == AppointmentStatus.Cancelled && appointment.CancelledAt is null)
        {
            appointment.CancelledAt = DateTimeOffset.UtcNow;
        }

        unitOfWork.Repository<Appointment>().Update(appointment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AppointmentResponse>.Success(ToResponse(appointment));
    }

    public async Task<ServiceResult<IReadOnlyList<AppointmentNoteResponse>>> GetNotesAsync(Guid appointmentId, Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default)
    {
        var appointment = await GetAppointmentForMutationAsync(appointmentId, cancellationToken);
        if (appointment is null || !CanManageAppointment(appointment, userId, roles))
        {
            return ServiceResult<IReadOnlyList<AppointmentNoteResponse>>.NotFound();
        }

        var notes = await unitOfWork.Repository<AppointmentNote>()
            .Query()
            .Where(note => note.AppointmentId == appointmentId)
            .Include(note => note.Author)
            .OrderByDescending(note => note.CreatedAt)
            .Select(note => ToNoteResponse(note))
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<AppointmentNoteResponse>>.Success(notes);
    }

    public async Task<ServiceResult<AppointmentNoteResponse>> AddNoteAsync(Guid appointmentId, Guid authorId, IReadOnlyCollection<string> roles, CreateAppointmentNoteRequest request, CancellationToken cancellationToken = default)
    {
        if (!roles.Contains(RoleNames.Admin) && !roles.Contains(RoleNames.Staff))
        {
            return ServiceResult<AppointmentNoteResponse>.ValidationError("Only admin and staff can add appointment notes.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return ServiceResult<AppointmentNoteResponse>.ValidationError("Note body is required.");
        }

        var appointment = await GetAppointmentForMutationAsync(appointmentId, cancellationToken);
        if (appointment is null || !CanManageAppointment(appointment, authorId, roles))
        {
            return ServiceResult<AppointmentNoteResponse>.NotFound();
        }

        var author = await unitOfWork.Repository<AppUser>().GetByIdAsync(authorId, cancellationToken);
        if (author is null)
        {
            return ServiceResult<AppointmentNoteResponse>.NotFound("Author was not found.");
        }

        var note = new AppointmentNote
        {
            AppointmentId = appointmentId,
            Appointment = appointment,
            AuthorId = authorId,
            Author = author,
            Body = request.Body.Trim()
        };

        await unitOfWork.Repository<AppointmentNote>().AddAsync(note, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<AppointmentNoteResponse>.Success(ToNoteResponse(note));
    }

    private IQueryable<Appointment> AppointmentQuery() =>
        unitOfWork.Repository<Appointment>()
            .Query()
            .Include(appointment => appointment.Customer)
            .Include(appointment => appointment.Service)
            .Include(appointment => appointment.StaffProfile)
            .ThenInclude(staff => staff.User);

    private async Task<ServiceResult<PagedResponse<AppointmentResponse>>> ToPagedAppointmentsAsync(IQueryable<Appointment> query, AppointmentQueryRequest request, CancellationToken cancellationToken)
    {
        AppointmentStatus status = default;
        if (!string.IsNullOrWhiteSpace(request.Status) && !Enum.TryParse<AppointmentStatus>(request.Status, true, out status))
        {
            return ServiceResult<PagedResponse<AppointmentResponse>>.ValidationError("Appointment status filter is invalid.");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(appointment => appointment.Status == status);
        }

        if (request.FromDate.HasValue)
        {
            var from = businessClock.LocalDateTimeToUtc(request.FromDate.Value, TimeOnly.MinValue);
            query = query.Where(appointment => appointment.StartsAt >= from);
        }

        if (request.ToDate.HasValue)
        {
            var to = businessClock.LocalDateTimeToUtc(request.ToDate.Value.AddDays(1), TimeOnly.MinValue);
            query = query.Where(appointment => appointment.StartsAt < to);
        }

        if (request.StaffId.HasValue)
        {
            query = query.Where(appointment => appointment.StaffProfileId == request.StaffId.Value);
        }

        if (request.ServiceId.HasValue)
        {
            query = query.Where(appointment => appointment.ServiceId == request.ServiceId.Value);
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(appointment => appointment.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(appointment => ToResponse(appointment))
            .ToListAsync(cancellationToken);

        return ServiceResult<PagedResponse<AppointmentResponse>>.Success(new PagedResponse<AppointmentResponse>(items, page, pageSize, total));
    }

    private async Task<bool> IsWithinAvailabilityAsync(Guid staffProfileId, DateTimeOffset startsAt, DateTimeOffset endsAt, CancellationToken cancellationToken)
    {
        var startLocal = businessClock.UtcToLocalParts(startsAt);
        var endLocal = businessClock.UtcToLocalParts(endsAt);
        var startTime = startLocal.Time;
        var endTime = endLocal.Time;
        var dayOfWeek = startLocal.Date.DayOfWeek;

        return await unitOfWork.Repository<StaffAvailability>().AnyAsync(item => item.StaffProfileId == staffProfileId && item.DayOfWeek == dayOfWeek && item.IsActive && item.StartTime <= startTime && item.EndTime >= endTime, cancellationToken);
    }

    private async Task<bool> HasStaffOverlapAsync(Guid staffProfileId, DateTimeOffset startsAt, DateTimeOffset endsAt, Guid? excludeAppointmentId, CancellationToken cancellationToken)
    {
        return await unitOfWork.Repository<Appointment>().AnyAsync(appointment => appointment.StaffProfileId == staffProfileId && (!excludeAppointmentId.HasValue || appointment.Id != excludeAppointmentId.Value) && BlockingStatuses.Contains(appointment.Status) && startsAt < appointment.EndsAt && endsAt > appointment.StartsAt, cancellationToken);
    }

    private async Task<Appointment?> GetAppointmentForMutationAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        return await AppointmentQuery().SingleOrDefaultAsync(appointment => appointment.Id == appointmentId, cancellationToken);
    }

    private static bool CanManageAppointment(Appointment appointment, Guid userId, IReadOnlyCollection<string> roles)
    {
        return roles.Contains(RoleNames.Admin)
            || appointment.CustomerId == userId
            || (roles.Contains(RoleNames.Staff) && appointment.StaffProfile.UserId == userId);
    }

    private static Notification CreateNotification(AppUser? user, Appointment appointment, string type, string subject, string body) =>
        new()
        {
            UserId = user?.Id ?? appointment.CustomerId,
            Appointment = appointment,
            Type = type,
            Recipient = user?.Email ?? string.Empty,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending
        };

    private static AppointmentResponse ToResponse(Appointment appointment) =>
        new(appointment.Id, appointment.CustomerId, appointment.StaffProfileId, appointment.StaffProfile.User.DisplayName, appointment.ServiceId, appointment.Service.Name, appointment.StartsAt, appointment.EndsAt, appointment.Status.ToString());

    private static AppointmentNoteResponse ToNoteResponse(AppointmentNote note) =>
        new(note.Id, note.AppointmentId, note.AuthorId, note.Author.DisplayName, note.Body, note.CreatedAt);
}
