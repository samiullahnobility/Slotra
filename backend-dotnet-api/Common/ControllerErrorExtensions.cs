using Microsoft.AspNetCore.Mvc;

namespace Slotra.Api.Common;

public static class ControllerErrorExtensions
{
    public static ObjectResult Error(this ControllerBase controller, int statusCode, string? message, IEnumerable<string>? errors = null) =>
        controller.StatusCode(statusCode, new ApiErrorResponse(message ?? "Request failed.", errors, controller.HttpContext.TraceIdentifier));
}
