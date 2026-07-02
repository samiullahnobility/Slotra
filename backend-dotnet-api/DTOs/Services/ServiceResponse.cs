namespace Slotra.Api.DTOs.Services;

public sealed record ServiceResponse(Guid Id, string Name, string? Description, int DurationMinutes, decimal Price, bool IsActive);
