using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Slotra.Api.Models;

namespace Slotra.Api.Data;

public sealed class SlotraDbContext(DbContextOptions<SlotraDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Service> Services => Set<Service>();

    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();

    public DbSet<StaffService> StaffServices => Set<StaffService>();

    public DbSet<StaffAvailability> StaffAvailability => Set<StaffAvailability>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    public DbSet<AppointmentNote> AppointmentNotes => Set<AppointmentNote>();

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(entity =>
        {
            entity.Property(user => user.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.CreatedAt).IsRequired();
            entity.Property(user => user.RefreshToken).HasMaxLength(256);
        });

        builder.Entity<Service>(entity =>
        {
            entity.Property(service => service.Name).HasMaxLength(120).IsRequired();
            entity.Property(service => service.Description).HasMaxLength(1000);
            entity.Property(service => service.Price).HasColumnType("decimal(18,2)");
            entity.Property(service => service.CreatedAt).IsRequired();
            entity.Property(service => service.UpdatedAt);
            entity.HasIndex(service => service.Name).IsUnique();
        });

        builder.Entity<StaffProfile>(entity =>
        {
            entity.Property(staff => staff.Bio).HasMaxLength(1000);
            entity.Property(staff => staff.CreatedAt).IsRequired();
            entity.Property(staff => staff.UpdatedAt);
            entity.HasIndex(staff => staff.UserId).IsUnique();
            entity.HasOne(staff => staff.User)
                .WithOne()
                .HasForeignKey<StaffProfile>(staff => staff.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StaffService>(entity =>
        {
            entity.HasKey(staffService => new { staffService.StaffProfileId, staffService.ServiceId });
            entity.Property(staffService => staffService.CreatedAt).IsRequired();
            entity.Property(staffService => staffService.UpdatedAt);
            entity.HasOne(staffService => staffService.StaffProfile)
                .WithMany(staff => staff.StaffServices)
                .HasForeignKey(staffService => staffService.StaffProfileId);
            entity.HasOne(staffService => staffService.Service)
                .WithMany(service => service.StaffServices)
                .HasForeignKey(staffService => staffService.ServiceId);
        });

        builder.Entity<StaffAvailability>(entity =>
        {
            entity.Property(availability => availability.CreatedAt).IsRequired();
            entity.Property(availability => availability.UpdatedAt);
            entity.HasIndex(availability => new { availability.StaffProfileId, availability.DayOfWeek, availability.StartTime, availability.EndTime });
            entity.HasOne(availability => availability.StaffProfile)
                .WithMany(staff => staff.Availability)
                .HasForeignKey(availability => availability.StaffProfileId);
        });

        builder.Entity<Appointment>(entity =>
        {
            entity.Property(appointment => appointment.CancellationReason).HasMaxLength(500);
            entity.Property(appointment => appointment.CreatedAt).IsRequired();
            entity.Property(appointment => appointment.UpdatedAt);
            entity.HasIndex(appointment => new { appointment.StaffProfileId, appointment.StartsAt, appointment.EndsAt });
            entity.HasIndex(appointment => new { appointment.CustomerId, appointment.StartsAt });
            entity.HasOne(appointment => appointment.Customer)
                .WithMany()
                .HasForeignKey(appointment => appointment.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(appointment => appointment.StaffProfile)
                .WithMany(staff => staff.Appointments)
                .HasForeignKey(appointment => appointment.StaffProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(appointment => appointment.Service)
                .WithMany(service => service.Appointments)
                .HasForeignKey(appointment => appointment.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AppointmentNote>(entity =>
        {
            entity.Property(note => note.Body).HasMaxLength(4000).IsRequired();
            entity.Property(note => note.CreatedAt).IsRequired();
            entity.Property(note => note.UpdatedAt);
            entity.HasOne(note => note.Appointment)
                .WithMany(appointment => appointment.Notes)
                .HasForeignKey(note => note.AppointmentId);
            entity.HasOne(note => note.Author)
                .WithMany()
                .HasForeignKey(note => note.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.Property(notification => notification.Type).HasMaxLength(80).IsRequired();
            entity.Property(notification => notification.Recipient).HasMaxLength(256).IsRequired();
            entity.Property(notification => notification.Subject).HasMaxLength(200).IsRequired();
            entity.Property(notification => notification.Body).HasMaxLength(4000).IsRequired();
            entity.Property(notification => notification.ErrorMessage).HasMaxLength(1000);
            entity.Property(notification => notification.CreatedAt).IsRequired();
            entity.Property(notification => notification.UpdatedAt);
            entity.HasIndex(notification => new { notification.Status, notification.CreatedAt });
            entity.HasOne(notification => notification.User)
                .WithMany()
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(notification => notification.Appointment)
                .WithMany(appointment => appointment.Notifications)
                .HasForeignKey(notification => notification.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

