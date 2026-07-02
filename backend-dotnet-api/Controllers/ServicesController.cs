using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Common;
using Slotra.Api.DTOs.Services;
using Slotra.Api.Models;
using Slotra.Api.Services;

namespace Slotra.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class ServicesController(IServiceManagementService serviceManagementService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<ServiceResponse>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await serviceManagementService.GetAllAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ServiceResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var service = await serviceManagementService.GetByIdAsync(id, cancellationToken);
        return service is null ? NotFound() : Ok(service);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<ActionResult<ServiceResponse>> Create(CreateServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await serviceManagementService.CreateAsync(request, cancellationToken);

        return result.Status switch
        {
            ServiceResultStatus.Success => CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value),
            ServiceResultStatus.Conflict => this.Error(StatusCodes.Status409Conflict, result.Error),
            ServiceResultStatus.ValidationError => this.Error(StatusCodes.Status400BadRequest, result.Error),
            _ => this.Error(StatusCodes.Status400BadRequest, result.Error)
        };
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Update(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken)
    {
        var result = await serviceManagementService.UpdateAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await serviceManagementService.DeleteAsync(id, cancellationToken);
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



