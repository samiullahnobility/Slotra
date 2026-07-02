using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Slotra.Api.Models;

namespace Slotra.Api.Data.Seed;

public static class SlotraIdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        foreach (var roleName in RoleNames.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }

        var adminEmail = configuration["SeedAdmin:Email"] ?? "admin@slotra.local";
        var adminPassword = configuration["SeedAdmin:Password"] ?? "Admin123!";
        var normalizedAdminEmail = adminEmail.Trim().ToLowerInvariant();

        var adminUser = await userManager.FindByEmailAsync(normalizedAdminEmail);
        if (adminUser is null)
        {
            adminUser = new AppUser
            {
                UserName = normalizedAdminEmail,
                Email = normalizedAdminEmail,
                EmailConfirmed = true,
                DisplayName = "Slotra Admin"
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed admin user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, RoleNames.Admin))
        {
            await userManager.AddToRoleAsync(adminUser, RoleNames.Admin);
        }

        await SeedDemoDataAsync(services, userManager, configuration);
    }

    private static async Task SeedDemoDataAsync(IServiceProvider services, UserManager<AppUser> userManager, IConfiguration configuration)
    {
        var dbContext = services.GetRequiredService<SlotraDbContext>();

        var serviceName = configuration["SeedDemo:ServiceName"] ?? "Dental Cleaning";
        var service = await dbContext.Services.SingleOrDefaultAsync(item => item.Name == serviceName);
        if (service is null)
        {
            service = new Service
            {
                Name = serviceName,
                Description = "Routine cleaning appointment for MVP demo bookings.",
                DurationMinutes = 60,
                Price = 120,
                IsActive = true
            };

            dbContext.Services.Add(service);
            await dbContext.SaveChangesAsync();
        }

        var staffEmail = (configuration["SeedDemo:StaffEmail"] ?? "dr.smith@slotra.local").Trim().ToLowerInvariant();
        var staffPassword = configuration["SeedDemo:StaffPassword"] ?? "Staff123!";
        var staffUser = await userManager.FindByEmailAsync(staffEmail);
        if (staffUser is null)
        {
            staffUser = new AppUser
            {
                UserName = staffEmail,
                Email = staffEmail,
                EmailConfirmed = true,
                DisplayName = "Dr. Smith"
            };

            var staffCreateResult = await userManager.CreateAsync(staffUser, staffPassword);
            if (!staffCreateResult.Succeeded)
            {
                var errors = string.Join(", ", staffCreateResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed staff user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(staffUser, RoleNames.Staff))
        {
            await userManager.AddToRoleAsync(staffUser, RoleNames.Staff);
        }

        var staffProfile = await dbContext.StaffProfiles.SingleOrDefaultAsync(profile => profile.UserId == staffUser.Id);
        if (staffProfile is null)
        {
            staffProfile = new StaffProfile
            {
                UserId = staffUser.Id,
                Bio = "Demo staff member for appointment booking.",
                IsActive = true
            };

            dbContext.StaffProfiles.Add(staffProfile);
            await dbContext.SaveChangesAsync();
        }

        if (!await dbContext.StaffServices.AnyAsync(item => item.StaffProfileId == staffProfile.Id && item.ServiceId == service.Id))
        {
            dbContext.StaffServices.Add(new StaffService
            {
                StaffProfileId = staffProfile.Id,
                ServiceId = service.Id
            });
        }

        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            var hasAvailability = await dbContext.StaffAvailability.AnyAsync(item => item.StaffProfileId == staffProfile.Id && item.DayOfWeek == day);
            if (!hasAvailability)
            {
                dbContext.StaffAvailability.Add(new StaffAvailability
                {
                    StaffProfileId = staffProfile.Id,
                    DayOfWeek = day,
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(17, 0),
                    IsActive = true
                });
            }
        }

        var customerEmail = (configuration["SeedDemo:CustomerEmail"] ?? "customer@slotra.local").Trim().ToLowerInvariant();
        var customerPassword = configuration["SeedDemo:CustomerPassword"] ?? "Customer123!";
        var customerUser = await userManager.FindByEmailAsync(customerEmail);
        if (customerUser is null)
        {
            customerUser = new AppUser
            {
                UserName = customerEmail,
                Email = customerEmail,
                EmailConfirmed = true,
                DisplayName = "Demo Customer"
            };

            var customerCreateResult = await userManager.CreateAsync(customerUser, customerPassword);
            if (!customerCreateResult.Succeeded)
            {
                var errors = string.Join(", ", customerCreateResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Failed to seed customer user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(customerUser, RoleNames.Customer))
        {
            await userManager.AddToRoleAsync(customerUser, RoleNames.Customer);
        }

        await dbContext.SaveChangesAsync();
    }
}

