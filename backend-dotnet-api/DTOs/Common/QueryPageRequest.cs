namespace Slotra.Api.DTOs.Common;

public sealed record QueryPageRequest(int Page = 1, int PageSize = 25);
