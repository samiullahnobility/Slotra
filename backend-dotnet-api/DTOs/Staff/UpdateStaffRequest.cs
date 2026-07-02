namespace Slotra.Api.DTOs.Staff;

public sealed record UpdateStaffRequest(string DisplayName, string? Bio, bool IsActive);
