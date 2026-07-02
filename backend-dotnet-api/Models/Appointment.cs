namespace Slotra.Api.Models;

public sealed class Appointment
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public AppUser Customer { get; set; } = null!;

    public Guid StaffProfileId { get; set; }

    public StaffProfile StaffProfile { get; set; } = null!;

    public Guid ServiceId { get; set; }

    public Service Service { get; set; } = null!;

    public DateTimeOffset StartsAt { get; set; }

    public DateTimeOffset EndsAt { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }

    public ICollection<AppointmentNote> Notes { get; set; } = new List<AppointmentNote>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
