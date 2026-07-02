namespace Slotra.Api.Models;

public sealed class AppointmentNote
{
    public Guid Id { get; set; }

    public Guid AppointmentId { get; set; }

    public Appointment Appointment { get; set; } = null!;

    public Guid AuthorId { get; set; }

    public AppUser Author { get; set; } = null!;

    public required string Body { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
