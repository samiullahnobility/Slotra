using Slotra.Api.Common;
using Slotra.Api.DTOs.Common;
using Slotra.Api.DTOs.Staff;

namespace Slotra.Api.Services;

public interface IStaffManagementService
{
    Task<IReadOnlyList<StaffResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PagedResponse<StaffResponse>> GetPagedAsync(QueryPageRequest request, CancellationToken cancellationToken = default);

    Task<StaffResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<StaffResponse?> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Guid id, UpdateStaffRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffServiceResponse>?> GetServicesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult> AssignServiceAsync(Guid id, AssignStaffServiceRequest request, CancellationToken cancellationToken = default);

    Task<bool> RemoveServiceAsync(Guid id, Guid serviceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffAvailabilityResponse>?> GetAvailabilityAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<StaffAvailabilityResponse>> AddAvailabilityAsync(Guid id, CreateStaffAvailabilityRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAvailabilityAsync(Guid id, Guid availabilityId, UpdateStaffAvailabilityRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAvailabilityAsync(Guid id, Guid availabilityId, CancellationToken cancellationToken = default);
}

