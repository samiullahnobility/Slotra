namespace Slotra.Api.Common;

public sealed record ApiErrorResponse(string Message, IEnumerable<string>? Errors = null, string? TraceId = null);
