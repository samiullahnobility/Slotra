namespace Slotra.Api.Models;

public sealed class StaffProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public AppUser User { get; set; } = null!;

    public string? Bio { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<StaffService> StaffServices { get; set; } = new List<StaffService>();

    public ICollection<StaffAvailability> Availability { get; set; } = new List<StaffAvailability>();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
