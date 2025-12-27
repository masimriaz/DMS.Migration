namespace DMS.Migration.Application.Common.Models;

/// <summary>
/// Represents the result of an operation with success/failure state and optional error information
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error != null)
            throw new InvalidOperationException("Successful result cannot have an error");
        if (!isSuccess && error == null)
            throw new InvalidOperationException("Failed result must have an error");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>
/// Represents the result of an operation that returns a value
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result");

    internal Result(T? value, bool isSuccess, Error? error)
        : base(isSuccess, error)
    {
        _value = value;
    }
}

/// <summary>
/// Represents an error with code and message
/// </summary>
public record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);

    // Common errors
    public static Error NotFound(string entityName, object id) =>
        new("NotFound", $"{entityName} with id '{id}' was not found", ErrorType.NotFound);

    public static Error Validation(string message) =>
        new("Validation", message, ErrorType.Validation);

    public static Error Conflict(string message) =>
        new("Conflict", message, ErrorType.Conflict);

    public static Error Unauthorized(string message = "Unauthorized access") =>
        new("Unauthorized", message, ErrorType.Unauthorized);

    public static Error TenantMismatch(int expectedTenant, int actualTenant) =>
        new("TenantMismatch", $"Resource belongs to tenant {actualTenant}, but request is for tenant {expectedTenant}", ErrorType.Unauthorized);
}

public enum ErrorType
{
    None = 0,
    Failure = 1,
    Validation = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5
}
