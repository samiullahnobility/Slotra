using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Slotra.Api.DTOs.Auth;
using Slotra.Api.Models;

namespace Slotra.Api.Services;

public sealed class AuthService(
    UserManager<AppUser> userManager,
    IConfiguration configuration) : IAuthService
{
    public async Task<AuthUserResponse?> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return null;
        }

        await userManager.AddToRoleAsync(user, RoleNames.Customer);

        return new AuthUserResponse(user.Id, user.Email!, user.DisplayName, [RoleNames.Customer]);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);

        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return null;
        }

        return await CreateAuthResponseAsync(user);
    }

    public async Task<AuthResponse?> RefreshAsync(RefreshTokenRequest request)
    {
        var user = userManager.Users.SingleOrDefault(user =>
            user.RefreshToken == request.RefreshToken &&
            user.RefreshTokenExpiresAt > DateTimeOffset.UtcNow);

        return user is null ? null : await CreateAuthResponseAsync(user);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(AppUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        var token = CreateJwtToken(user, roles);
        var refreshToken = CreateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
        await userManager.UpdateAsync(user);

        return new AuthResponse(
            token,
            refreshToken,
            user.RefreshTokenExpiresAt.Value,
            new AuthUserResponse(user.Id, user.Email!, user.DisplayName, roles));
    }

    private string CreateJwtToken(AppUser user, IEnumerable<string> roles)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new("displayName", user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiresInMinutes = int.TryParse(jwtSettings["ExpiresInMinutes"], out var configuredMinutes)
            ? configuredMinutes
            : 60;

        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateRefreshToken()
    {
        Span<byte> bytes = stackalloc byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
