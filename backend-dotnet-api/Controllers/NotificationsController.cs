using Microsoft.AspNetCore.Mvc;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Notifications;
using Slotra.Api.Services;

namespace Slotra.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController(INotificationService notificationService, IConfiguration configuration) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> GetPending([FromQuery] NotificationQueryRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsWorkerAuthorized()) return Unauthorized();
        return Ok(await notificationService.GetPendingAsync(request, cancellationToken));
    }

    [HttpPut("{id:guid}/sent")]
    public async Task<IActionResult> MarkSent(Guid id, CancellationToken cancellationToken)
    {
        if (!IsWorkerAuthorized()) return Unauthorized();
        return ToActionResult(await notificationService.MarkSentAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}/failed")]
    public async Task<IActionResult> MarkFailed(Guid id, MarkNotificationFailedRequest request, CancellationToken cancellationToken)
    {
        if (!IsWorkerAuthorized()) return Unauthorized();
        return ToActionResult(await notificationService.MarkFailedAsync(id, request, cancellationToken));
    }

    private IActionResult ToActionResult(ServiceResult result) =>
        result.Status switch
        {
            ServiceResultStatus.Success => NoContent(),
            ServiceResultStatus.NotFound => this.Error(StatusCodes.Status404NotFound, result.Error),
            ServiceResultStatus.ValidationError => this.Error(StatusCodes.Status400BadRequest, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };

    private bool IsWorkerAuthorized()
    {
        var configuredKey = configuration["Worker:ApiKey"];
        return !string.IsNullOrWhiteSpace(configuredKey)
            && Request.Headers.TryGetValue("X-Worker-Api-Key", out var providedKey)
            && providedKey == configuredKey;
    }
}


