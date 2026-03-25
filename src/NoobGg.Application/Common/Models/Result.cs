namespace NoobGg.Application.Common.Models;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public int StatusCode { get; }

    protected Result(bool isSuccess, string? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success() => new(true, null, 200);
    public static Result Fail(string error, int statusCode = 400) => new(false, error, statusCode);
}

public class Result<T> : Result
{
    public T? Data { get; }

    private Result(bool isSuccess, T? data, string? error, int statusCode)
        : base(isSuccess, error, statusCode)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(true, data, null, 200);
    public static Result<T> Created(T data) => new(true, data, null, 201);
    public static new Result<T> Fail(string error, int statusCode = 400) => new(false, default, error, statusCode);
    public static Result<T> NotFound(string error = "Resource not found") => new(false, default, error, 404);
    public static Result<T> Unauthorized(string error = "Unauthorized") => new(false, default, error, 401);
    public static Result<T> Forbidden(string error = "Forbidden") => new(false, default, error, 403);
}
