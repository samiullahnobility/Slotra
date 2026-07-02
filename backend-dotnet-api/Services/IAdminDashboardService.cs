using Slotra.Api.DTOs.Admin;

namespace Slotra.Api.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
}
