namespace Slotra.Api.Models;

public sealed class Notification
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public AppUser? User { get; set; }

    public Guid? AppointmentId { get; set; }

    public Appointment? Appointment { get; set; }

    public required string Type { get; set; }

    public required string Recipient { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public string? ErrorMessage { get; set; }
}
