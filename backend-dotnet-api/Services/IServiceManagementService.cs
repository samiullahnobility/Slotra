using Slotra.Api.Common;
using Slotra.Api.DTOs.Common;
using Slotra.Api.DTOs.Services;

namespace Slotra.Api.Services;

public interface IServiceManagementService
{
    Task<IReadOnlyList<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<PagedResponse<ServiceResponse>> GetPagedAsync(QueryPageRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult<ServiceResponse>> CreateAsync(CreateServiceRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(Guid id, UpdateServiceRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

