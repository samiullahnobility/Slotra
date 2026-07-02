namespace Slotra.Api.DTOs.Auth;

public sealed record AuthResponse(string Token, string RefreshToken, DateTimeOffset RefreshTokenExpiresAt, AuthUserResponse User);
