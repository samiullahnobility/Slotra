using Microsoft.EntityFrameworkCore;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Notifications;
using Slotra.Api.Models;
using Slotra.Api.UnitOfWork;

namespace Slotra.Api.Services;

public sealed class NotificationService(IUnitOfWork unitOfWork) : INotificationService
{
    public async Task<PagedResponse<NotificationResponse>> GetPendingAsync(NotificationQueryRequest request, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = unitOfWork.Repository<Notification>()
            .Query()
            .Where(notification => notification.Status == NotificationStatus.Pending);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(notification => notification.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(notification => ToResponse(notification))
            .ToListAsync(cancellationToken);

        return new PagedResponse<NotificationResponse>(items, page, pageSize, total);
    }

    public async Task<ServiceResult> MarkSentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await unitOfWork.Repository<Notification>().GetByIdAsync(id, cancellationToken);
        if (notification is null)
        {
            return ServiceResult.NotFound();
        }

        notification.Status = NotificationStatus.Sent;
        notification.SentAt = DateTimeOffset.UtcNow;
        notification.ErrorMessage = null;

        unitOfWork.Repository<Notification>().Update(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> MarkFailedAsync(Guid id, MarkNotificationFailedRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ErrorMessage))
        {
            return ServiceResult.ValidationError("Error message is required.");
        }

        var notification = await unitOfWork.Repository<Notification>().GetByIdAsync(id, cancellationToken);
        if (notification is null)
        {
            return ServiceResult.NotFound();
        }

        notification.Status = NotificationStatus.Failed;
        notification.ErrorMessage = request.ErrorMessage.Trim();

        unitOfWork.Repository<Notification>().Update(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }

    private static NotificationResponse ToResponse(Notification notification) =>
        new(
            notification.Id,
            notification.Type,
            notification.Recipient,
            notification.Subject,
            notification.Body,
            notification.Status.ToString(),
            notification.CreatedAt,
            notification.SentAt,
            notification.ErrorMessage);
}

