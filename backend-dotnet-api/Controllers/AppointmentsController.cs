using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Appointments;
using Slotra.Api.Services;

namespace Slotra.Api.Controllers;

[ApiController]
[Route("api/v1/appointments")]
[Authorize]
public sealed class AppointmentsController(IBookingService bookingService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AppointmentResponse>>> GetAll([FromQuery] AppointmentQueryRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await bookingService.GetAppointmentsAsync(userId.Value, GetRoles(), request, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(result.Value),
            ServiceResultStatus.ValidationError => this.Error(StatusCodes.Status400BadRequest, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };
    }

    [HttpGet("my")]
    public async Task<ActionResult<IReadOnlyList<AppointmentResponse>>> GetMy(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(await bookingService.GetMyAppointmentsAsync(userId.Value, cancellationToken));
    }

    [HttpPost]
    [EnableRateLimiting("Booking")]
    public async Task<ActionResult<AppointmentResponse>> Create(CreateAppointmentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await bookingService.CreateAppointmentAsync(userId.Value, GetRoles(), request, cancellationToken);
        return ToAppointmentActionResult(result, created: true);
    }

    [HttpPut("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancelAppointmentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await bookingService.CancelAppointmentAsync(id, userId.Value, GetRoles(), request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/reschedule")]
    public async Task<ActionResult<AppointmentResponse>> Reschedule(Guid id, RescheduleAppointmentRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await bookingService.RescheduleAppointmentAsync(id, userId.Value, GetRoles(), request, cancellationToken);
        return ToAppointmentActionResult(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<AppointmentResponse>> UpdateStatus(Guid id, UpdateAppointmentStatusRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await bookingService.UpdateStatusAsync(id, userId.Value, GetRoles(), request, cancellationToken);
        return ToAppointmentActionResult(result);
    }

    [HttpGet("{id:guid}/notes")]
    public async Task<ActionResult<IReadOnlyList<AppointmentNoteResponse>>> GetNotes(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await bookingService.GetNotesAsync(id, userId.Value, GetRoles(), cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(result.Value),
            ServiceResultStatus.NotFound => this.Error(StatusCodes.Status404NotFound, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<AppointmentNoteResponse>> AddNote(Guid id, CreateAppointmentNoteRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var result = await bookingService.AddNoteAsync(id, userId.Value, GetRoles(), request, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(result.Value),
            ServiceResultStatus.NotFound => this.Error(StatusCodes.Status404NotFound, result.Error),
            ServiceResultStatus.ValidationError => this.Error(StatusCodes.Status400BadRequest, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private IReadOnlyCollection<string> GetRoles() =>
        User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();

    private ActionResult<AppointmentResponse> ToAppointmentActionResult(ServiceResult<AppointmentResponse> result, bool created = false) =>
        result.Status switch
        {
            ServiceResultStatus.Success when created => CreatedAtAction(nameof(GetAll), new { id = result.Value!.Id }, result.Value),
            ServiceResultStatus.Success => Ok(result.Value),
            ServiceResultStatus.NotFound => this.Error(StatusCodes.Status404NotFound, result.Error),
            ServiceResultStatus.Conflict => this.Error(StatusCodes.Status409Conflict, result.Error),
            ServiceResultStatus.ValidationError => this.Error(StatusCodes.Status400BadRequest, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };

    private IActionResult ToActionResult(ServiceResult result) =>
        result.Status switch
        {
            ServiceResultStatus.Success => NoContent(),
            ServiceResultStatus.NotFound => this.Error(StatusCodes.Status404NotFound, result.Error),
            ServiceResultStatus.Conflict => this.Error(StatusCodes.Status409Conflict, result.Error),
            ServiceResultStatus.ValidationError => this.Error(StatusCodes.Status400BadRequest, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };
}





