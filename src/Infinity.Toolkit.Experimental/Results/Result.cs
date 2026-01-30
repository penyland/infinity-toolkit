namespace Infinity.Toolkit.Experimental.Results;

/// <summary>
/// Represents the result of an operation that returns a value and indicates success or failure.
/// </summary>
/// <remarks>Use this type to encapsulate both the outcome of an operation and its return value, allowing callers
/// to check for success and access the result in a type-safe manner. If the operation fails, the result contains error
/// information and the value is not meaningful.</remarks>
/// <typeparam name="T">The type of the value returned by a successful operation.</typeparam>
public class Result<T>
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
        {
            throw new ArgumentException("Invalid error", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    private Result(T value, bool isSuccess, Error error) : this(isSuccess, error) => Value = value;

    private Result(Error error) : this(false, error) { }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public T? Value => IsSuccess ? field : throw new InvalidOperationException("Can't access Value when IsSuccess is false");

    public static Result<T> Success(T value) => new(value, true, Error.None);

    public static Result<T> Failure(Error error) => new(error);

    public static Result<T> Failure(Exception e) => new(new ExceptionError(e));

    public static implicit operator T?(Result<T> result)
    {
        return result.Value;
    }
}

/// <summary>
/// Represents an error with a code and a descriptive message.
/// </summary>
/// <remarks>Use the static <see cref="None"/> field to represent the absence of an error. This record is
/// immutable and can be used to convey error information in a consistent manner throughout an application.</remarks>
/// <param name="Code">The error code that identifies the type or category of the error. Cannot be null.</param>
/// <param name="Description">A human-readable description of the error. Cannot be null.</param>
public record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

/// <summary>
/// Represents an error that encapsulates an exception instance, providing access to the original exception details.
/// </summary>
/// <remarks>Use this type to propagate exception information through error-handling APIs that operate on the
/// Error abstraction. The original exception is accessible via the Exception property for inspection or logging
/// purposes.</remarks>
public record ExceptionError : Error
{
    public ExceptionError(Exception exception) : base(exception.GetType().Name, exception.Message) => Exception = exception;

    public Exception Exception { get; }
}
