namespace Slotra.Api.DTOs.Notifications;

public sealed record NotificationQueryRequest(int Page = 1, int PageSize = 25);
