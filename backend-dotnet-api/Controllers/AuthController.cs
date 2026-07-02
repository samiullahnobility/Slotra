using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Slotra.Api.Common;
using Slotra.Api.DTOs.Auth;
using Slotra.Api.Services;

namespace Slotra.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("Auth")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthUserResponse>> Register(RegisterRequest request)
    {
        var user = await authService.RegisterAsync(request);
        if (user is null)
        {
            return this.Error(StatusCodes.Status400BadRequest, "Registration failed.");
        }

        return Created($"/api/users/{user.Id}", user);
    }

    [HttpPost("login")]
    [EnableRateLimiting("Auth")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var auth = await authService.LoginAsync(request);
        if (auth is null)
        {
            return Unauthorized();
        }

        return Ok(auth);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("Auth")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request)
    {
        var auth = await authService.RefreshAsync(request);
        return auth is null ? Unauthorized() : Ok(auth);
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<object> Me()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToArray();

        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email),
            displayName = User.FindFirstValue("displayName"),
            roles
        });
    }
}




