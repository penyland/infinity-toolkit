namespace Infinity.Toolkit;

public abstract class ResultBase
{
    public bool Succeeded { get; protected set; } = true;

    public bool Failed => !Succeeded;

    public IReadOnlyCollection<Error> Errors { get; protected set; } = [];
}

public abstract class Result : ResultBase
{
    public static Result Success() => new SuccessResult();

    public static Result BadRequest(string message) => new ErrorResult(new Error(message, string.Empty, ErrorType.Validation));

    public static Result Failure(string message) => new ErrorResult(message);

    public static Result Failure(Error error) => new ErrorResult(error);

    /// <summary>
    /// Converts a <see cref="Result"/> instance to an array of <see cref="Error"/> objects, representing the errors
    /// contained in the result.
    /// </summary>
    /// <remarks>If the specified <paramref name="result"/> is not an <see cref="ErrorResult"/>, the returned
    /// array contains a single <see cref="Error.None"/> value to indicate the absence of errors.</remarks>
    /// <param name="result">The <see cref="Result"/> to convert. If the result contains errors, they will be returned in the array;
    /// otherwise, a default error is returned.</param>
    public static implicit operator Error[](Result result)
    {
        if (result is ErrorResult errorResult)
        {
            return [.. errorResult.Errors];
        }

        return [Error.None]; // [] if no errors are present.
    }

    public static Result Failure(Result result)
    {
        if (result is ErrorResult errorResult)
        {
            return new ErrorResult(errorResult.Errors);
        }

        return new ErrorResult(result.Errors);
    }

    public static Result Failure(params Result[] results)
    {
        if (results.Length == 0)
        {
            return new ErrorResult("No errors provided.");
        }

        var errors = results.SelectMany(r => r.Errors).ToList();
        return new ErrorResult(errors);
    }

    /// <summary>
    /// Creates a new <see cref="Result"/> that combines the errors of the given <paramref name="result"/> with the provided <paramref name="error"/>. 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    public static Result Failure(Result result, Error error)
    {
        if (result is ErrorResult errorResult)
        {
            return new ErrorResult([.. errorResult.Errors, error]);
        }
        return new ErrorResult([.. result.Errors, error]);
    }
}

public abstract class Result<T> : ResultBase
{
    private T value;

    internal Result(T value, IReadOnlyCollection<Error> errors)
    {
        this.value = value;
        Errors = errors;
        Succeeded = errors.Count == 0;
    }

    public T Value
    {
        get => Succeeded ? value : throw new InvalidOperationException($"You can't access .{nameof(Value)} when .{nameof(Succeeded)} is false");
        set => this.value = value;
    }

    public static Result<T> Failure(Result<T> result, Error error)
    {
        if (result is ErrorResult<T> errorResult)
        {
            return new ErrorResult<T>([.. errorResult.Errors, error]);
        }
        return new ErrorResult<T>(result.Errors);
    }

    //public static Result<T> Success() => new SuccessResult<T>(default!);

    public static Result<T> Success(T data) => new SuccessResult<T>(data);

    public static Result<T> Failure(string details) => new ErrorResult<T>(details);

    public static Result<T> Failure(Error error) => new ErrorResult<T>(error);

    public static Result<T> Failure(IReadOnlyCollection<Error> errors) => new ErrorResult<T>(errors);

    public static Result<T> Failure(string message, IReadOnlyCollection<Error> errors) => new ErrorResult<T>(message, errors);

    public static Result<T> Failure(Exception exception) => new ErrorResult<T>(exception);

    public static Result<T> BadRequest(string details) => new ErrorResult<T>(new Error("BadRequest", details, ErrorType.Validation));

    public static Result<T> NotFound(string details) => new ErrorResult<T>(new Error("NotFound", details, ErrorType.NotFound));

    public static Result<T> Unauthorized(string details) => new ErrorResult<T>(new Error("Unauthorized", details, ErrorType.Unauthorized));

    public static Result<T> Forbidden(string details) => new ErrorResult<T>(new Error("Forbidden", details, ErrorType.Forbidden));

    public static Result<T> Conflict(string details) => new ErrorResult<T>(new Error("Conflict", details, ErrorType.Conflict));

    public static Result<T> InternalError(string details) => new ErrorResult<T>(new Error("InternalError", details, ErrorType.Failure));

    public static Result<T> ValidationError(string propertyName, string errorMessage) => new ErrorResult<T>(new ValidationError(propertyName, errorMessage));

    /// <summary>
    /// Implicitly converts a value of type <typeparamref name="T"/> to a <see cref="Result{T}"/> representing a
    /// successful result.
    /// </summary>
    /// <remarks>This operator allows direct assignment of a value of type <typeparamref name="T"/> to a <see
    /// cref="Result{T}"/> variable, treating the value as a successful result. This can simplify code when working with
    /// APIs that return <see cref="Result{T}"/>.</remarks>
    /// <param name="value">The value to be wrapped in a <see cref="SuccessResult{T}"/>.</param>
    public static implicit operator Result<T>(T value) => new SuccessResult<T>(value);

    /// <summary>
    /// Converts an <see cref="Error"/> instance to a <see cref="Result{T}"/> representing a failed result.
    /// </summary>
    /// <remarks>This operator enables implicit conversion from <see cref="Error"/> to <see
    /// cref="Result{T}"/>, allowing error values to be returned directly where a result is expected. The resulting <see
    /// cref="Result{T}"/> will indicate failure and contain the provided error.</remarks>
    /// <param name="error">The error information to associate with the failed result. Cannot be null.</param>
    public static implicit operator Result<T>(Error error) => new ErrorResult<T>(error);

    /// <summary>
    /// Converts an <see cref="Exception"/> to a <see cref="Result{T}"/> representing a failed operation.
    /// </summary>
    /// <remarks>This operator allows exceptions to be implicitly converted to error results, enabling
    /// streamlined error handling in result-based workflows. The resulting <see cref="Result{T}"/> will indicate
    /// failure and contain the provided exception.</remarks>
    /// <param name="exception">The exception that describes the error condition to be encapsulated in the result. Cannot be null.</param>
    public static implicit operator Result<T>(Exception exception) => new ErrorResult<T>(exception);

    /// <summary>
    /// Implicitly converts a <see cref="Result{T}"/> instance to its contained value of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>If the <paramref name="result"/> represents a failure, accessing its value may throw an
    /// exception depending on the implementation of <see cref="Result{T}"/>. Use this conversion only when you are
    /// certain the result is successful.</remarks>
    /// <param name="result">The <see cref="Result{T}"/> instance to convert.</param>
    public static implicit operator T(Result<T> result) => result.Value;
}

#pragma warning disable IDE1006 // Naming Styles
public interface Success { }

public interface Failure { }

#pragma warning restore IDE1006 // Naming Styles
