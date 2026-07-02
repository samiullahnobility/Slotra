using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Common;
using Slotra.Api.DTOs.Staff;
using Slotra.Api.Models;
using Slotra.Api.Services;

namespace Slotra.Api.Controllers;

[ApiController]
[Route("api/v1/staff")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class StaffController(IStaffManagementService staffManagementService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StaffResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await staffManagementService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StaffResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var staff = await staffManagementService.GetByIdAsync(id, cancellationToken);
        return staff is null ? NotFound() : Ok(staff);
    }

    [HttpPost]
    public async Task<ActionResult<StaffResponse>> Create(CreateStaffRequest request, CancellationToken cancellationToken)
    {
        var staff = await staffManagementService.CreateAsync(request, cancellationToken);
        return staff is null ? this.Error(StatusCodes.Status400BadRequest, "Staff user could not be created.") : CreatedAtAction(nameof(GetById), new { id = staff.Id }, staff);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateStaffRequest request, CancellationToken cancellationToken)
    {
        var updated = await staffManagementService.UpdateAsync(id, request, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await staffManagementService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/services")]
    public async Task<ActionResult<IReadOnlyList<StaffServiceResponse>>> GetServices(Guid id, CancellationToken cancellationToken)
    {
        var services = await staffManagementService.GetServicesAsync(id, cancellationToken);
        return services is null ? NotFound() : Ok(services);
    }

    [HttpPost("{id:guid}/services")]
    public async Task<IActionResult> AssignService(Guid id, AssignStaffServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await staffManagementService.AssignServiceAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}/services/{serviceId:guid}")]
    public async Task<IActionResult> RemoveService(Guid id, Guid serviceId, CancellationToken cancellationToken)
    {
        var removed = await staffManagementService.RemoveServiceAsync(id, serviceId, cancellationToken);
        return removed ? NoContent() : NotFound();
    }

    [HttpGet("{id:guid}/availability")]
    public async Task<ActionResult<IReadOnlyList<StaffAvailabilityResponse>>> GetAvailability(Guid id, CancellationToken cancellationToken)
    {
        var availability = await staffManagementService.GetAvailabilityAsync(id, cancellationToken);
        return availability is null ? NotFound() : Ok(availability);
    }

    [HttpPost("{id:guid}/availability")]
    public async Task<ActionResult<StaffAvailabilityResponse>> AddAvailability(Guid id, CreateStaffAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var result = await staffManagementService.AddAvailabilityAsync(id, request, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => Ok(result.Value),
            ServiceResultStatus.NotFound => this.Error(StatusCodes.Status404NotFound, result.Error),
            ServiceResultStatus.ValidationError => this.Error(StatusCodes.Status400BadRequest, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };
    }

    [HttpPut("{id:guid}/availability/{availabilityId:guid}")]
    public async Task<IActionResult> UpdateAvailability(Guid id, Guid availabilityId, UpdateStaffAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var result = await staffManagementService.UpdateAvailabilityAsync(id, availabilityId, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}/availability/{availabilityId:guid}")]
    public async Task<IActionResult> DeleteAvailability(Guid id, Guid availabilityId, CancellationToken cancellationToken)
    {
        var result = await staffManagementService.DeleteAvailabilityAsync(id, availabilityId, cancellationToken);
        return ToActionResult(result);
    }

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



