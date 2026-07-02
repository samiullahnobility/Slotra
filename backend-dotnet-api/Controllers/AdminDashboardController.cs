using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotra.Api.DTOs.Admin;
using Slotra.Api.Models;
using Slotra.Api.Services;

namespace Slotra.Api.Controllers;

[ApiController]
[Route("api/v1/admin/dashboard")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminDashboardController(IAdminDashboardService adminDashboardService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<AdminDashboardSummaryResponse>> GetSummary(CancellationToken cancellationToken) =>
        Ok(await adminDashboardService.GetSummaryAsync(cancellationToken));
}


