using System.Collections.Immutable;

namespace Infinity.Toolkit.Experimental.Results;

/// <summary>
/// Represents the result of an operation that returns a value and indicates success or failure.
/// </summary>
/// <remarks>Use this type to encapsulate both the outcome of an operation and its return value, allowing callers
/// to check for success and access the result in a type-safe manner. If the operation fails, the result contains error
/// information and the value is not meaningful.</remarks>
/// <typeparam name="T">The type of the value returned by a successful operation.</typeparam>
public sealed class Result<T>
{
    private Result(bool isSuccess = true, params Error[] errors)
    {
        if (isSuccess && errors != null && errors.Length > 0 || !isSuccess && (errors == null || errors.Length == 0))
        {
            throw new ArgumentException("Invalid error", nameof(errors));
        }

        Errors = errors != null ? ImmutableList.Create(errors) : [];
        IsSuccess = Errors.Count == 0;
    }

    private Result(T value) : this(true) => Value = value;

    private Result(params Error[] error) : this(false, error) { }

    public bool IsSuccess { get; }

    public ImmutableList<Error> Errors { get; init; }

    public T? Value => IsSuccess ? field : throw new InvalidOperationException("Can't access Value when Result is a failure");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(params Error[] errors) => new(errors);

    /// <summary>
    /// Implicitly converts a <see cref="Result{T}"/> to its underlying value of type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <param name="result">The <see cref="Result{T}"/> instance to be converted.</param>
    public static implicit operator T?(Result<T> result) => result.Value;

    /// <summary>
    /// Implicitly converts a value of type <typeparamref name="T"/> to a <see cref="Result{T}"/>
    /// representing a successful result.
    /// </summary>
    /// <param name="value">The value to be wrapped in a successful <see cref="Result{T}"/>.</param>
    public static implicit operator Result<T>(T value) => new(value);
}

/// <summary>
/// Represents an error with a code and a descriptive message.
/// </summary>
/// <remarks>This record is immutable and can be used to convey error information in a consistent manner throughout an application.</remarks>
/// <param name="Code">The error code that identifies the type or category of the error. Cannot be null.</param>
/// <param name="Description">A human-readable description of the error. Cannot be null.</param>
public record Error(int Code, string Description);

public record BadRequestError(string Description) : Error(400, Description);
public record ValidationError(string Description) : Error(400, Description);
public record UnauthorizedError(string Description) : Error(401, Description);
public record NotFoundError(string Description) : Error(404, Description);
public record ConflictError(string Description) : Error(409, Description);
public record InternalServerError(string Description) : Error(500, Description);

/// <summary>
/// Represents an error that encapsulates an exception instance, providing access to the original exception details.
/// </summary>
/// <remarks>Use this type to propagate exception information through error-handling APIs that operate on the
/// Error abstraction. The original exception is accessible via the Exception property for inspection or logging
/// purposes.</remarks>
public record ExceptionError : InternalServerError
{
    public ExceptionError(Exception exception) : base(exception.Message) => Exception = exception;

    public Exception Exception { get; }
}

public static class Result
{
    public static Result<T> Failure<T>(Exception e) => Result<T>.Failure(new ExceptionError(e));

    // Add error to existing result
    public static Result<T> Failure<T>(Result<T> result, Error error)
    {
        if (result is Result<T> errorResult)
        {
            return Result<T>.Failure([.. errorResult.Errors, error]);
        }
    
        return Result<T>.Failure([.. result.Errors]);
    }

}
