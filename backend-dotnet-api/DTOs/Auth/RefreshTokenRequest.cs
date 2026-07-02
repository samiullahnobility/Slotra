using System.ComponentModel.DataAnnotations;

namespace Slotra.Api.DTOs.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}
