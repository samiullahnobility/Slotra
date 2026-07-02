using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Booking;
using Slotra.Api.Services;

namespace Slotra.Api.Controllers;

[ApiController]
[Route("api/v1/booking")]
public sealed class BookingController(IBookingService bookingService) : ControllerBase
{
    [HttpGet("services")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<BookingServiceResponse>>> GetServices(CancellationToken cancellationToken) =>
        Ok(await bookingService.GetServicesAsync(cancellationToken));

    [HttpGet("staff")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<AvailableStaffResponse>>> GetAvailableStaff([FromQuery] Guid serviceId, CancellationToken cancellationToken) =>
        Ok(await bookingService.GetAvailableStaffAsync(serviceId, cancellationToken));

    [HttpGet("available-slots")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<AvailableSlotResponse>>> GetAvailableSlots(
        [FromQuery] Guid serviceId,
        [FromQuery] DateOnly date,
        [FromQuery] Guid? staffId,
        CancellationToken cancellationToken)
    {
        var result = await bookingService.GetAvailableSlotsAsync(serviceId, date, staffId, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(result.Value),
            ServiceResultStatus.NotFound => this.Error(StatusCodes.Status404NotFound, result.Error),
            ServiceResultStatus.ValidationError => this.Error(StatusCodes.Status400BadRequest, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };
    }
}


