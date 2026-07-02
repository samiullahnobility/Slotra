using System.ComponentModel.DataAnnotations;

namespace Slotra.Api.DTOs.Services;

public sealed class UpdateServiceRequest
{
    [Required, MaxLength(120)]
    public required string Name { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Range(1, 1440)]
    public int DurationMinutes { get; init; }

    [Range(0, 999999)]
    public decimal Price { get; init; }

    public bool IsActive { get; init; }
}
