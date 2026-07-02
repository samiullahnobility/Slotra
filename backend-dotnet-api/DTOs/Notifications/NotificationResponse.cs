namespace Slotra.Api.DTOs.Notifications;

public sealed record NotificationResponse(
    Guid Id,
    string Type,
    string Recipient,
    string Subject,
    string Body,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt,
    string? ErrorMessage);
