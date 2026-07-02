namespace Slotra.Api.Common;

public enum ServiceResultStatus
{
    Success,
    NotFound,
    ValidationError,
    Conflict
}

public sealed record ServiceResult<T>(ServiceResultStatus Status, T? Value = default, string? Error = null)
{
    public static ServiceResult<T> Success(T value) => new(ServiceResultStatus.Success, value);

    public static ServiceResult<T> NotFound(string? error = null) => new(ServiceResultStatus.NotFound, default, error);

    public static ServiceResult<T> ValidationError(string error) => new(ServiceResultStatus.ValidationError, default, error);

    public static ServiceResult<T> Conflict(string error) => new(ServiceResultStatus.Conflict, default, error);
}

public sealed record ServiceResult(ServiceResultStatus Status, string? Error = null)
{
    public static ServiceResult Success() => new(ServiceResultStatus.Success);

    public static ServiceResult NotFound(string? error = null) => new(ServiceResultStatus.NotFound, error);

    public static ServiceResult ValidationError(string error) => new(ServiceResultStatus.ValidationError, error);

    public static ServiceResult Conflict(string error) => new(ServiceResultStatus.Conflict, error);
}
