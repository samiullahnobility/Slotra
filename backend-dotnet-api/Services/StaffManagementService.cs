using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Common;
using Slotra.Api.DTOs.Staff;
using Slotra.Api.Models;
using Slotra.Api.UnitOfWork;

namespace Slotra.Api.Services;

public sealed class StaffManagementService(
    UserManager<AppUser> userManager,
    IUnitOfWork unitOfWork) : IStaffManagementService
{
    public async Task<IReadOnlyList<StaffResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var staff = await unitOfWork.Repository<StaffProfile>()
            .Query()
            .Include(profile => profile.User)
            .OrderBy(profile => profile.User.DisplayName)
            .ToListAsync(cancellationToken);

        return staff.Select(ToResponse).ToList();
    }

    public async Task<PagedResponse<StaffResponse>> GetPagedAsync(QueryPageRequest request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = unitOfWork.Repository<StaffProfile>()
            .Query()
            .Include(profile => profile.User)
            .OrderBy(profile => profile.User.DisplayName);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(profile => ToResponse(profile))
            .ToListAsync(cancellationToken);

        return new PagedResponse<StaffResponse>(items, page, pageSize, total);
    }

    public async Task<StaffResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var staff = await unitOfWork.Repository<StaffProfile>()
            .Query()
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken);

        return staff is null ? null : ToResponse(staff);
    }

    public async Task<StaffResponse?> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return null;
        }

        await userManager.AddToRoleAsync(user, RoleNames.Staff);

        var profile = new StaffProfile
        {
            UserId = user.Id,
            User = user,
            Bio = request.Bio?.Trim()
        };

        await unitOfWork.Repository<StaffProfile>().AddAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(profile);
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateStaffRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return false;
        }

        var staff = await unitOfWork.Repository<StaffProfile>()
            .Query()
            .Include(profile => profile.User)
            .SingleOrDefaultAsync(profile => profile.Id == id, cancellationToken);

        if (staff is null)
        {
            return false;
        }

        staff.User.DisplayName = request.DisplayName.Trim();
        staff.Bio = request.Bio?.Trim();
        staff.IsActive = request.IsActive;

        unitOfWork.Repository<StaffProfile>().Update(staff);
        await userManager.UpdateAsync(staff.User);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var staff = await unitOfWork.Repository<StaffProfile>().GetByIdAsync(id, cancellationToken);
        if (staff is null)
        {
            return false;
        }

        staff.IsActive = false;
        unitOfWork.Repository<StaffProfile>().Update(staff);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StaffServiceResponse>?> GetServicesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var staffExists = await unitOfWork.Repository<StaffProfile>().AnyAsync(staff => staff.Id == id, cancellationToken);
        if (!staffExists)
        {
            return null;
        }

        var services = await unitOfWork.Repository<StaffService>()
            .Query()
            .Where(staffService => staffService.StaffProfileId == id && staffService.IsActive)
            .Include(staffService => staffService.Service)
            .OrderBy(staffService => staffService.Service.Name)
            .Select(staffService => new StaffServiceResponse(
                staffService.ServiceId,
                staffService.Service.Name,
                staffService.Service.DurationMinutes,
                staffService.Service.Price,
                staffService.Service.IsActive))
            .ToListAsync(cancellationToken);

        return services;
    }

    public async Task<ServiceResult> AssignServiceAsync(Guid id, AssignStaffServiceRequest request, CancellationToken cancellationToken = default)
    {
        var staffExists = await unitOfWork.Repository<StaffProfile>().AnyAsync(staff => staff.Id == id, cancellationToken);
        var serviceExists = await unitOfWork.Repository<Service>().AnyAsync(service => service.Id == request.ServiceId, cancellationToken);

        if (!staffExists || !serviceExists)
        {
            return ServiceResult.NotFound();
        }

        var existingAssignment = await unitOfWork.Repository<StaffService>()
            .Query()
            .SingleOrDefaultAsync(staffService => staffService.StaffProfileId == id && staffService.ServiceId == request.ServiceId, cancellationToken);

        if (existingAssignment?.IsActive == true)
        {
            return ServiceResult.Conflict("Staff member is already assigned to this service.");
        }

        if (existingAssignment is not null)
        {
            existingAssignment.IsActive = true;
            existingAssignment.UpdatedAt = DateTimeOffset.UtcNow;
            unitOfWork.Repository<StaffService>().Update(existingAssignment);
        }
        else
        {
            await unitOfWork.Repository<StaffService>().AddAsync(new StaffService
            {
                StaffProfileId = id,
                ServiceId = request.ServiceId
            }, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<bool> RemoveServiceAsync(Guid id, Guid serviceId, CancellationToken cancellationToken = default)
    {
        var staffService = await unitOfWork.Repository<StaffService>()
            .Query()
            .SingleOrDefaultAsync(existing => existing.StaffProfileId == id && existing.ServiceId == serviceId, cancellationToken);

        if (staffService is null)
        {
            return false;
        }

        staffService.IsActive = false;
        unitOfWork.Repository<StaffService>().Update(staffService);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StaffAvailabilityResponse>?> GetAvailabilityAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var staffExists = await unitOfWork.Repository<StaffProfile>().AnyAsync(staff => staff.Id == id, cancellationToken);
        if (!staffExists)
        {
            return null;
        }

        var availability = await unitOfWork.Repository<StaffAvailability>()
            .Query()
            .Where(item => item.StaffProfileId == id)
            .OrderBy(item => item.DayOfWeek)
            .ThenBy(item => item.StartTime)
            .Select(item => new StaffAvailabilityResponse(item.Id, item.DayOfWeek, item.StartTime, item.EndTime, item.IsActive))
            .ToListAsync(cancellationToken);

        return availability;
    }

    public async Task<ServiceResult<StaffAvailabilityResponse>> AddAvailabilityAsync(Guid id, CreateStaffAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAvailability(request.StartTime, request.EndTime);
        if (validationError is not null)
        {
            return ServiceResult<StaffAvailabilityResponse>.ValidationError(validationError);
        }

        var staffExists = await unitOfWork.Repository<StaffProfile>().AnyAsync(staff => staff.Id == id, cancellationToken);
        if (!staffExists)
        {
            return ServiceResult<StaffAvailabilityResponse>.NotFound();
        }

        if (await HasAvailabilityOverlapAsync(id, request.DayOfWeek, request.StartTime, request.EndTime, null, cancellationToken))
        {
            return ServiceResult<StaffAvailabilityResponse>.Conflict("Availability overlaps an existing active availability window.");
        }

        var availability = new StaffAvailability
        {
            StaffProfileId = id,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsActive = request.IsActive
        };

        await unitOfWork.Repository<StaffAvailability>().AddAsync(availability, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<StaffAvailabilityResponse>.Success(ToAvailabilityResponse(availability));
    }

    public async Task<ServiceResult> UpdateAvailabilityAsync(Guid id, Guid availabilityId, UpdateStaffAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAvailability(request.StartTime, request.EndTime);
        if (validationError is not null)
        {
            return ServiceResult.ValidationError(validationError);
        }

        var availability = await unitOfWork.Repository<StaffAvailability>()
            .Query()
            .SingleOrDefaultAsync(item => item.Id == availabilityId && item.StaffProfileId == id, cancellationToken);

        if (availability is null)
        {
            return ServiceResult.NotFound();
        }

        if (await HasAvailabilityOverlapAsync(id, request.DayOfWeek, request.StartTime, request.EndTime, availabilityId, cancellationToken))
        {
            return ServiceResult.Conflict("Availability overlaps an existing active availability window.");
        }

        availability.DayOfWeek = request.DayOfWeek;
        availability.StartTime = request.StartTime;
        availability.EndTime = request.EndTime;
        availability.IsActive = request.IsActive;
        availability.UpdatedAt = DateTimeOffset.UtcNow;

        unitOfWork.Repository<StaffAvailability>().Update(availability);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAvailabilityAsync(Guid id, Guid availabilityId, CancellationToken cancellationToken = default)
    {
        var availability = await unitOfWork.Repository<StaffAvailability>()
            .Query()
            .SingleOrDefaultAsync(item => item.Id == availabilityId && item.StaffProfileId == id, cancellationToken);

        if (availability is null)
        {
            return ServiceResult.NotFound();
        }

        availability.IsActive = false;
        availability.UpdatedAt = DateTimeOffset.UtcNow;
        unitOfWork.Repository<StaffAvailability>().Update(availability);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static string? ValidateAvailability(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            return "Availability end time must be after start time.";
        }

        return null;
    }

    private async Task<bool> HasAvailabilityOverlapAsync(Guid staffProfileId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, Guid? excludeAvailabilityId, CancellationToken cancellationToken)
    {
        return await unitOfWork.Repository<StaffAvailability>().AnyAsync(availability =>
            availability.StaffProfileId == staffProfileId &&
            availability.DayOfWeek == dayOfWeek &&
            availability.IsActive &&
            (!excludeAvailabilityId.HasValue || availability.Id != excludeAvailabilityId.Value) &&
            startTime < availability.EndTime &&
            endTime > availability.StartTime,
            cancellationToken);
    }

    private static StaffResponse ToResponse(StaffProfile staff) =>
        new(staff.Id, staff.UserId, staff.User.Email!, staff.User.DisplayName, staff.Bio, staff.IsActive);

    private static StaffAvailabilityResponse ToAvailabilityResponse(StaffAvailability availability) =>
        new(availability.Id, availability.DayOfWeek, availability.StartTime, availability.EndTime, availability.IsActive);
}
