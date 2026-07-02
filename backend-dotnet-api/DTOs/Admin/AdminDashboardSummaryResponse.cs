namespace Slotra.Api.DTOs.Admin;

public sealed record AdminDashboardSummaryResponse(
    int TotalAppointments,
    int TodayAppointments,
    int CompletedAppointments,
    int CancelledAppointments,
    decimal EstimatedRevenue);
