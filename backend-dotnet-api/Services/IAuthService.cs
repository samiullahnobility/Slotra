using Slotra.Api.DTOs.Auth;

namespace Slotra.Api.Services;

public interface IAuthService
{
    Task<AuthUserResponse?> RegisterAsync(RegisterRequest request);

    Task<AuthResponse?> LoginAsync(LoginRequest request);

    Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request);
}
