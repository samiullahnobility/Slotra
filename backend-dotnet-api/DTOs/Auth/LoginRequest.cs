using System.ComponentModel.DataAnnotations;

namespace Slotra.Api.DTOs.Auth;

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    [Required, MaxLength(128)]
    public required string Password { get; init; }
}
