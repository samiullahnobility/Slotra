namespace Slotra.Api.DTOs.Staff;

public sealed record StaffResponse(Guid Id, Guid UserId, string Email, string DisplayName, string? Bio, bool IsActive);
