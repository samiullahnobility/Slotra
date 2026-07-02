using System.ComponentModel.DataAnnotations;

namespace Slotra.Api.DTOs.Auth;

public sealed class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public required string Email { get; init; }

    [Required, MinLength(8), MaxLength(128)]
    public required string Password { get; init; }

    [Required, Compare(nameof(Password))]
    public required string ConfirmPassword { get; init; }

    [Required, MaxLength(120)]
    public required string DisplayName { get; init; }
}
