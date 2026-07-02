namespace Slotra.Api.DTOs.Auth;

public sealed record AuthUserResponse(Guid Id, string Email, string DisplayName, IEnumerable<string> Roles);
