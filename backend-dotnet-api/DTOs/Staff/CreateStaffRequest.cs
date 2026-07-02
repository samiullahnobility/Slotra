namespace Slotra.Api.DTOs.Staff;

public sealed record CreateStaffRequest(string Email, string Password, string DisplayName, string? Bio);
