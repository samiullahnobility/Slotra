using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Appointments;
using Slotra.Api.Models;
using Slotra.Api.Services;

namespace Slotra.Api.Controllers;

[ApiController]
[Route("api/v1/staff/me")]
[Authorize(Roles = RoleNames.Staff)]
public sealed class StaffMeController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("appointments/today")]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> GetTodayAppointments(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Unauthorized();
        }

        var result = await bookingService.GetStaffTodayAppointmentsAsync(userId, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(result.Value),
            ServiceResultStatus.NotFound => this.Error(StatusCodes.Status404NotFound, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };
    }
}


