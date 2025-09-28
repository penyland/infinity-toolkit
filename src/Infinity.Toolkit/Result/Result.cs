namespace Infinity.Toolkit;

public abstract class Result
{
    public bool Succeeded { get; protected set; } = true;

    public bool Failed => !Succeeded;

    public IReadOnlyCollection<Error> Errors { get; protected set; } = [Error.None];

    public static Result Success() => new SuccessResult();

    public static Result<T> Ok<T>(T value) => new SuccessResult<T>(value);

    public static Result<T> Success<T>(T data) => new SuccessResult<T>(data);

    public static Result<T> Success<T>() => new SuccessResult<T>();

    public static Result Failure(string message) => new ErrorResult(message);

    public static Result Failure(Error error) => new ErrorResult(error);

    public static Result<T> Failure<T>(string details) => new ErrorResult<T>(details);

    public static Result<T> Failure<T>(Error error) => new ErrorResult<T>(error);

    public static Result<T> Failure<T>(string message, IReadOnlyCollection<Error> errors) => new ErrorResult<T>(message, errors);

    public static Result<T> Failure<T>(Exception exception) => new ErrorResult<T>(exception);

    /// <summary>
    /// Implicitly extracts <see cref="Error"/>s (if any) from a <see cref="Result"/> instance. 
    /// </summary>
    /// <param name="result"></param>
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

public abstract class Result<T> : Result
{
    private T value;

    protected Result(T value, IReadOnlyCollection<Error> errors)
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

    public static implicit operator Result<T>(T value) => new SuccessResult<T>(value);

    public static implicit operator Result<T>(Error error) => new ErrorResult<T>(error);

    public static implicit operator Result<T>(Exception exception) => new ErrorResult<T>(exception);

    public static implicit operator T(Result<T> result) => result.Value;

    public TResult Switch<TResult>(Func<T, TResult> onSuccess, Func<IReadOnlyCollection<Error>, TResult> onFailure)
    {
        return Succeeded ? onSuccess(Value!) : onFailure(Errors);
    }
}

#pragma warning disable SA1302 // Interface names should begin with I
#pragma warning disable IDE1006 // Naming Styles
public interface Success { }

public interface Failure { }

#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1302 // Interface names should begin with I
