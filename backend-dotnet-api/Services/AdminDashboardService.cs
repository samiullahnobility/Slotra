using Microsoft.EntityFrameworkCore;
using Slotra.Api.DTOs.Admin;
using Slotra.Api.Models;
using Slotra.Api.UnitOfWork;

namespace Slotra.Api.Services;

public sealed class AdminDashboardService(IUnitOfWork unitOfWork, IBusinessClock businessClock) : IAdminDashboardService
{
    public async Task<AdminDashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var range = businessClock.LocalDateRangeToUtc(businessClock.Today);
        var dayStart = range[0];
        var dayEnd = range[1];

        var appointments = unitOfWork.Repository<Appointment>().Query();

        var totalAppointments = await appointments.CountAsync(cancellationToken);
        var todayAppointments = await appointments.CountAsync(appointment => appointment.StartsAt >= dayStart && appointment.StartsAt < dayEnd, cancellationToken);
        var completedAppointments = await appointments.CountAsync(appointment => appointment.Status == AppointmentStatus.Completed, cancellationToken);
        var cancelledAppointments = await appointments.CountAsync(appointment => appointment.Status == AppointmentStatus.Cancelled, cancellationToken);
        var estimatedRevenue = await appointments
            .Where(appointment => appointment.Status == AppointmentStatus.Completed)
            .SumAsync(appointment => appointment.Service.Price, cancellationToken);

        return new AdminDashboardSummaryResponse(
            totalAppointments,
            todayAppointments,
            completedAppointments,
            cancelledAppointments,
            estimatedRevenue);
    }
}
