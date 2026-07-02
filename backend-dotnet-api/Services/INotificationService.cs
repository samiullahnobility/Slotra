using Slotra.Api.Common;
using Slotra.Api.DTOs.Notifications;

namespace Slotra.Api.Services;

public interface INotificationService
{
    Task<PagedResponse<NotificationResponse>> GetPendingAsync(NotificationQueryRequest request, CancellationToken cancellationToken = default);

    Task<ServiceResult> MarkSentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ServiceResult> MarkFailedAsync(Guid id, MarkNotificationFailedRequest request, CancellationToken cancellationToken = default);
}

