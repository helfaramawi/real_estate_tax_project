namespace RealEstateTax.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    public IEnumerable<string> ValidationErrors { get; private set; } = [];
    public string? ErrorCode { get; private set; }

    private Result() { }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };

    public static Result<T> Failure(string error, string? errorCode = null) =>
        new() { IsSuccess = false, Error = error, ErrorCode = errorCode };

    public static Result<T> ValidationFailure(IEnumerable<string> errors) =>
        new() { IsSuccess = false, ValidationErrors = errors, ErrorCode = "VALIDATION_ERROR" };

    public static Result<T> NotFound(string message = "Resource not found") =>
        new() { IsSuccess = false, Error = message, ErrorCode = "NOT_FOUND" };

    public static Result<T> Forbidden(string message = "Access denied") =>
        new() { IsSuccess = false, Error = message, ErrorCode = "FORBIDDEN" };
}

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
